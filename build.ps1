param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("dev", "prod", "clean", "rebuild")]
    [string]$Mode = "dev",
    
    [Parameter(Mandatory=$false)]
    [switch]$NoCache,
    
    [Parameter(Mandatory=$false)]
    [switch]$Parallel
)

Write-Host "🚀 Baseline API Build Script" -ForegroundColor Green
Write-Host "Mode: $Mode" -ForegroundColor Yellow

# Set Docker BuildKit for faster builds
$env:DOCKER_BUILDKIT = 1
$env:COMPOSE_DOCKER_CLI_BUILD = 1

function Clean-All {
    Write-Host "🧹 Cleaning all containers and images..." -ForegroundColor Cyan
    # Clean both development and production environments
    docker-compose -f docker-compose.dev.yml down --volumes --remove-orphans
    docker-compose down --volumes --remove-orphans
    docker system prune -f
    docker builder prune -f
}

function Build-Dev {
    Write-Host "🔨 Building development environment..." -ForegroundColor Cyan
    
    $buildArgs = @()
    if ($NoCache) {
        $buildArgs += "--no-cache"
    }
    if ($Parallel) {
        $buildArgs += "--parallel"
    }
    
    docker-compose -f docker-compose.dev.yml build $buildArgs
    docker-compose -f docker-compose.dev.yml up -d
}

function Build-Prod {
    Write-Host "🏭 Building production environment..." -ForegroundColor Cyan
    
    $buildArgs = @()
    if ($NoCache) {
        $buildArgs += "--no-cache"
    }
    if ($Parallel) {
        $buildArgs += "--parallel"
    }
    
    docker-compose build $buildArgs
    docker-compose up -d
}

function Rebuild-All {
    Write-Host "🔄 Rebuilding everything..." -ForegroundColor Cyan
    Clean-All
    Build-Prod
}

# Main execution
switch ($Mode.ToLower()) {
    "dev" {
        Build-Dev
        Write-Host "✅ Development environment ready!" -ForegroundColor Green
        Write-Host "🌐 API: http://localhost:8080" -ForegroundColor Blue
        Write-Host "📊 PostgreSQL: localhost:5432" -ForegroundColor Blue
        Write-Host "🔴 Redis: localhost:6379" -ForegroundColor Blue
        Write-Host "`n📝 Development Commands:" -ForegroundColor Magenta
        Write-Host "  docker-compose -f docker-compose.dev.yml logs -f api" -ForegroundColor Gray
        Write-Host "  docker-compose -f docker-compose.dev.yml exec api bash" -ForegroundColor Gray
        Write-Host "  docker-compose -f docker-compose.dev.yml down" -ForegroundColor Gray
    }
    "prod" {
        Build-Prod
        Write-Host "✅ Production environment ready!" -ForegroundColor Green
        Write-Host "🌐 API: http://localhost:8080" -ForegroundColor Blue
        Write-Host "📊 PostgreSQL: localhost:5432" -ForegroundColor Blue
        Write-Host "🔴 Redis: localhost:6379" -ForegroundColor Blue
        Write-Host "📈 Prometheus: http://localhost:9090" -ForegroundColor Blue
        Write-Host "📊 Grafana: http://localhost:3000" -ForegroundColor Blue
        Write-Host "📝 Seq: http://localhost:5341" -ForegroundColor Blue
        Write-Host "`n📝 Production Commands:" -ForegroundColor Magenta
        Write-Host "  docker-compose logs -f api" -ForegroundColor Gray
        Write-Host "  docker-compose exec api bash" -ForegroundColor Gray
        Write-Host "  docker-compose down" -ForegroundColor Gray
    }
    "clean" {
        Clean-All
        Write-Host "✅ Cleanup completed!" -ForegroundColor Green
    }
    "rebuild" {
        Rebuild-All
        Write-Host "✅ Rebuild completed!" -ForegroundColor Green
    }
}

Write-Host "`n🎯 Quick Commands:" -ForegroundColor Magenta
Write-Host "  .\build.ps1 -Mode dev     # Start development" -ForegroundColor Gray
Write-Host "  .\build.ps1 -Mode prod    # Start production" -ForegroundColor Gray
Write-Host "  .\build.ps1 -Mode clean   # Clean everything" -ForegroundColor Gray
Write-Host "  .\build.ps1 -Mode rebuild # Rebuild production" -ForegroundColor Gray
