<#
.SYNOPSIS
Сквозной смоук живого стенда: регистрация, фото, модерация, лента и «Продано».

.DESCRIPTION
Требует запущенные API и PostgreSQL (docker compose), а также демо-модератора.
Создаёт временного пользователя, подтверждает email через psql, подаёт объявление
с фотографией, одобряет его модератором, проверяет ленту и «Продано», затем удаляет
объявление и пользователя.

.EXAMPLE
powershell -File scripts/smoke.ps1
#>
param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$ModeratorEmail = "moderator@example.com",
    [string]$ModeratorPassword = "123789",
    [string]$DatabaseUser = "doska",
    [string]$DatabaseName = "doska"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http

$repoRoot = Split-Path -Parent $PSScriptRoot
$photoPath = Join-Path $PSScriptRoot "fixtures/photo.png"

if (-not (Test-Path $photoPath)) {
    throw "Не найден файл фикстуры: $photoPath"
}

$password = "smoke-123789"
$suffix = [guid]::NewGuid().ToString("N").Substring(0, 10)
$email = "smoke-$suffix@example.com"
$marker = "Smoke $suffix"
$adId = $null
$user = $null
$moderator = $null

function New-ApiClient {
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.UseCookies = $true
    $handler.CookieContainer = [System.Net.CookieContainer]::new()
    $client = [System.Net.Http.HttpClient]::new($handler)
    $client.BaseAddress = [uri]$BaseUrl
    return $client
}

function Get-CsrfToken([System.Net.Http.HttpClient]$Client) {
    $json = $Client.GetStringAsync("/api/auth/csrf").GetAwaiter().GetResult() | ConvertFrom-Json

    if (-not $json.token) {
        throw "Не удалось получить antiforgery-токен."
    }

    return $json.token
}

function Invoke-ApiGet {
    param([System.Net.Http.HttpClient]$Client, [string]$Path)

    $response = $Client.GetAsync($Path).GetAwaiter().GetResult()
    $content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()

    return [pscustomobject]@{ Status = [int]$response.StatusCode; Content = $content }
}

function Invoke-ApiJson {
    param(
        [System.Net.Http.HttpClient]$Client,
        [string]$Method,
        [string]$Path,
        $Body
    )

    $request = [System.Net.Http.HttpRequestMessage]::new(
        [System.Net.Http.HttpMethod]::new($Method),
        $Path)
    $request.Headers.Add("X-CSRF-TOKEN", (Get-CsrfToken $Client))

    if ($null -ne $Body) {
        $json = $Body | ConvertTo-Json -Compress
        $request.Content = [System.Net.Http.StringContent]::new(
            $json,
            [System.Text.Encoding]::UTF8,
            "application/json")
    }

    $response = $Client.SendAsync($request).GetAwaiter().GetResult()
    $content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()

    return [pscustomobject]@{ Status = [int]$response.StatusCode; Content = $content }
}

function Assert-Status($Response, [int]$Expected, [string]$Step) {
    if ($Response.Status -ne $Expected) {
        throw "Шаг «$Step»: ожидался код $Expected, получен $($Response.Status). Ответ: $($Response.Content)"
    }
}

function Invoke-Psql([string]$Sql) {
    Push-Location $repoRoot

    try {
        $output = docker compose exec -T postgres psql -U $DatabaseUser -d $DatabaseName -c $Sql 2>&1

        if ($LASTEXITCODE -ne 0) {
            throw "psql завершился с ошибкой: $output"
        }

        return $output
    }
    finally {
        Pop-Location
    }
}

try {
    Write-Host "1/8 Проверка /api/health"
    $healthClient = New-ApiClient

    try {
        $health = Invoke-ApiGet -Client $healthClient -Path "/api/health"
        Assert-Status $health 200 "health"
        Write-Host "   $($health.Content)"
    }
    finally {
        $healthClient.Dispose()
    }

    $user = New-ApiClient
    $moderator = New-ApiClient

    Write-Host "2/8 Регистрация $email"
    $registration = Invoke-ApiJson -Client $user -Method "POST" -Path "/api/auth/register" -Body @{
        email = $email
        password = $password
    }
    Assert-Status $registration 200 "register"

    Write-Host "3/8 Подтверждение email через psql"
    Invoke-Psql "UPDATE users SET is_email_confirmed = true WHERE email = '$email';" | Out-Null

    Write-Host "4/8 Подача объявления с фото"
    $cities = (Invoke-ApiGet -Client $user -Path "/api/catalog/cities").Content | ConvertFrom-Json
    $categories = (Invoke-ApiGet -Client $user -Path "/api/catalog/categories").Content | ConvertFrom-Json

    if (-not $cities -or -not $categories) {
        throw "В справочнике нет городов или подразделов."
    }

    $form = [System.Net.Http.MultipartFormDataContent]::new()
    $form.Add([System.Net.Http.StringContent]::new($marker), "title")
    $form.Add([System.Net.Http.StringContent]::new("Сквозной смоук: фото, модерация, продажа."), "description")
    $form.Add([System.Net.Http.StringContent]::new("1500"), "price")
    $form.Add([System.Net.Http.StringContent]::new($cities[0].id), "cityId")
    $form.Add([System.Net.Http.StringContent]::new($categories[0].id), "categoryId")
    $form.Add([System.Net.Http.StringContent]::new("+7 900 000-00-00"), "contacts")

    $photo = [System.Net.Http.ByteArrayContent]::new([System.IO.File]::ReadAllBytes($photoPath))
    $photo.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::new("image/png")
    $form.Add($photo, "photos", "photo.png")

    $createRequest = [System.Net.Http.HttpRequestMessage]::new(
        [System.Net.Http.HttpMethod]::Post,
        "/api/ads")
    $createRequest.Headers.Add("X-CSRF-TOKEN", (Get-CsrfToken $user))
    $createRequest.Content = $form

    $createResponse = $user.SendAsync($createRequest).GetAwaiter().GetResult()
    $createContent = $createResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult()

    if ([int]$createResponse.StatusCode -ne 201) {
        throw "Создание объявления: $([int]$createResponse.StatusCode). $createContent"
    }

    $created = $createContent | ConvertFrom-Json
    $adId = $created.id

    if ($created.status -ne "OnModeration") {
        throw "Ожидался статус OnModeration, получен $($created.status)."
    }

    if (@($created.photos).Count -lt 1) {
        throw "Объявление создано без фотографий."
    }

    Write-Host "   id=$adId, статус=$($created.status), фото=$(@($created.photos).Count)"

    Write-Host "5/8 Вход модератора и одобрение"
    $login = Invoke-ApiJson -Client $moderator -Method "POST" -Path "/api/auth/login" -Body @{
        email = $ModeratorEmail
        password = $ModeratorPassword
    }
    Assert-Status $login 200 "moderator login"

    $queue = (Invoke-ApiGet -Client $moderator -Path "/api/moderation/queue").Content | ConvertFrom-Json

    if (-not ($queue | Where-Object { $_.id -eq $adId })) {
        throw "Объявление не попало в очередь проверки."
    }

    $approved = Invoke-ApiJson -Client $moderator -Method "POST" -Path "/api/moderation/ads/$adId/approve" -Body $null
    Assert-Status $approved 200 "approve"

    if (($approved.Content | ConvertFrom-Json).status -ne "Active") {
        throw "После одобрения ожидался статус Active."
    }

    Write-Host "6/8 Проверка ленты"
    $feed = (Invoke-ApiGet -Client $user -Path ("/api/ads?query=" + [uri]::EscapeDataString($marker))).Content | ConvertFrom-Json

    if (-not ($feed.items | Where-Object { $_.id -eq $adId })) {
        throw "Одобренное объявление не найдено в ленте."
    }

    Write-Host "7/8 Отметка «Продано» и скрытие из ленты"
    $sold = Invoke-ApiJson -Client $user -Method "POST" -Path "/api/ads/$adId/sold" -Body $null
    Assert-Status $sold 200 "sold"

    $hiddenFeed = (Invoke-ApiGet -Client $user -Path ("/api/ads?query=" + [uri]::EscapeDataString($marker))).Content | ConvertFrom-Json

    if ($hiddenFeed.items | Where-Object { $_.id -eq $adId }) {
        throw "Проданное объявление осталось в ленте."
    }

    Write-Host "8/8 Удаление объявления и пользователя"
    $deleted = Invoke-ApiJson -Client $user -Method "DELETE" -Path "/api/ads/$adId" -Body $null
    Assert-Status $deleted 204 "delete ad"
    $adId = $null

    Invoke-Psql "DELETE FROM users WHERE email = '$email';" | Out-Null

    Write-Host "Смоук пройден: регистрация → подтверждение → фото → модерация → лента → продано → очистка." -ForegroundColor Green
}
finally {
    if ($adId) {
        try {
            Invoke-ApiJson -Client $user -Method "DELETE" -Path "/api/ads/$adId" -Body $null | Out-Null
        }
        catch {
            Write-Warning "Не удалось удалить тестовое объявление $adId."
        }
    }

    try {
        Invoke-Psql "DELETE FROM users WHERE email = '$email';" | Out-Null
    }
    catch {
        Write-Warning "Не удалось удалить тестового пользователя $email."
    }

    foreach ($client in @($user, $moderator)) {
        if ($client) {
            $client.Dispose()
        }
    }
}