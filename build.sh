#!/bin/bash

# Default values
MODE="dev"
NO_CACHE=false
PARALLEL=false

# Function to display usage
show_usage() {
    echo "🚀 Baseline API Build Script"
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -m, --mode MODE     Build mode: dev, prod, clean, rebuild (default: dev)"
    echo "  -n, --no-cache      Build without using cache"
    echo "  -p, --parallel      Build services in parallel"
    echo "  -h, --help          Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0                    # Start development environment"
    echo "  $0 -m prod           # Start production environment"
    echo "  $0 -m clean          # Clean everything"
    echo "  $0 -m rebuild        # Rebuild production"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -m|--mode)
            MODE="$2"
            shift 2
            ;;
        -n|--no-cache)
            NO_CACHE=true
            shift
            ;;
        -p|--parallel)
            PARALLEL=true
            shift
            ;;
        -h|--help)
            show_usage
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            show_usage
            exit 1
            ;;
    esac
done

# Validate mode
case $MODE in
    dev|prod|clean|rebuild)
        ;;
    *)
        echo "❌ Invalid mode: $MODE"
        echo "Valid modes: dev, prod, clean, rebuild"
        exit 1
        ;;
esac

echo "🚀 Baseline API Build Script"
echo "Mode: $MODE"

# Set Docker BuildKit for faster builds
export DOCKER_BUILDKIT=1
export COMPOSE_DOCKER_CLI_BUILD=1

# Function to clean all containers and images
clean_all() {
    echo "🧹 Cleaning all containers and images..."
    docker-compose -f docker-compose.dev.yml down --volumes --remove-orphans
    docker-compose down --volumes --remove-orphans
    docker system prune -f
    docker builder prune -f
}

# Function to build development environment
build_dev() {
    echo "🔨 Building development environment..."
    
    local build_args=""
    if [ "$NO_CACHE" = true ]; then
        build_args="$build_args --no-cache"
    fi
    if [ "$PARALLEL" = true ]; then
        build_args="$build_args --parallel"
    fi
    
    docker-compose -f docker-compose.dev.yml build $build_args
    docker-compose -f docker-compose.dev.yml up -d
}

# Function to build production environment
build_prod() {
    echo "🏭 Building production environment..."
    
    local build_args=""
    if [ "$NO_CACHE" = true ]; then
        build_args="$build_args --no-cache"
    fi
    if [ "$PARALLEL" = true ]; then
        build_args="$build_args --parallel"
    fi
    
    docker-compose build $build_args
    docker-compose up -d
}

# Function to rebuild everything
rebuild_all() {
    echo "🔄 Rebuilding everything..."
    clean_all
    build_prod
}

# Main execution
case $MODE in
    "dev")
        build_dev
        echo "✅ Development environment ready!"
        echo "🌐 API: http://localhost:8080"
        echo "📊 PostgreSQL: localhost:5432"
        echo "🔴 Redis: localhost:6379"
        echo ""
        echo "📝 Development Commands:"
        echo "  docker-compose -f docker-compose.dev.yml logs -f api"
        echo "  docker-compose -f docker-compose.dev.yml exec api bash"
        echo "  docker-compose -f docker-compose.dev.yml down"
        ;;
    "prod")
        build_prod
        echo "✅ Production environment ready!"
        echo "🌐 API: http://localhost:8080"
        echo "📊 PostgreSQL: localhost:5432"
        echo "🔴 Redis: localhost:6379"
        echo "📈 Prometheus: http://localhost:9090"
        echo "📊 Grafana: http://localhost:3000"
        echo "📝 Seq: http://localhost:5341"
        echo ""
        echo "📝 Production Commands:"
        echo "  docker-compose logs -f api"
        echo "  docker-compose exec api bash"
        echo "  docker-compose down"
        ;;
    "clean")
        clean_all
        echo "✅ Cleanup completed!"
        ;;
    "rebuild")
        rebuild_all
        echo "✅ Rebuild completed!"
        ;;
esac

echo ""
echo "🎯 Quick Commands:"
echo "  ./build.sh -m dev     # Start development"
echo "  ./build.sh -m prod    # Start production"
echo "  ./build.sh -m clean   # Clean everything"
echo "  ./build.sh -m rebuild # Rebuild production"
