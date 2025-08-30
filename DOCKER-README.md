# Docker Compose Setup

This project includes two Docker Compose configurations optimized for different environments.

## 🚀 Quick Start

### Using Build Script (Recommended)

```powershell
# Start development environment
.\build.ps1

# Start production environment
.\build.ps1 -Mode prod

# Clean everything
.\build.ps1 -Mode clean

# Rebuild production without cache
.\build.ps1 -Mode rebuild -NoCache
```

### Manual Docker Compose Commands

#### Development Environment

```bash
# Start development environment with hot reload
docker-compose -f docker-compose.dev.yml up -d

# View logs
docker-compose -f docker-compose.dev.yml logs -f api

# Stop development environment
docker-compose -f docker-compose.dev.yml down
```

#### Production Environment

```bash
# Start production environment with monitoring
docker-compose up -d

# View logs
docker-compose logs -f api

# Stop production environment
docker-compose down
```

## 📋 Environment Configurations

### Development (`docker-compose.dev.yml`)

- **Purpose**: Local development with hot reload
- **Services**: API, PostgreSQL, Redis
- **Features**:
  - Hot reload with `dotnet watch`
  - Source code mounted for live editing
  - Optimized for fast development cycles
  - PostgreSQL for consistent database experience

### Production (`docker-compose.yml`)

- **Purpose**: Production deployment with full monitoring
- **Services**: API, PostgreSQL, Redis, Seq, Prometheus, Grafana
- **Features**:
  - Production-optimized API build
  - Complete monitoring stack
  - Health checks for all services
  - Persistent data volumes

## 🔧 Services Overview

| Service    | Development | Production | Port | Purpose               |
| ---------- | ----------- | ---------- | ---- | --------------------- |
| API        | ✅          | ✅         | 8080 | .NET Web API          |
| PostgreSQL | ✅          | ✅         | 5432 | Database              |
| Redis      | ✅          | ✅         | 6379 | Caching               |
| Seq        | ❌          | ✅         | 5341 | Log aggregation       |
| Prometheus | ❌          | ✅         | 9090 | Metrics collection    |
| Grafana    | ❌          | ✅         | 3000 | Metrics visualization |

## 🌐 Service Endpoints

### API Endpoints

- **Health Check**: `http://localhost:8080/health`
- **API Documentation**: `http://localhost:8080/swagger`

### Monitoring Endpoints (Production Only)

- **Seq Logs**: `http://localhost:5341`
- **Prometheus**: `http://localhost:9090`
- **Grafana**: `http://localhost:3000` (admin/admin)

## 🔍 Health Checks

All services include health checks to ensure proper startup order:

- **API**: HTTP health check on `/health` endpoint
- **PostgreSQL**: `pg_isready` command
- **Redis**: `redis-cli ping` command
- **Prometheus**: Built-in health endpoint
- **Grafana**: Built-in health endpoint

## 📊 Monitoring Stack (Production)

### Seq - Log Aggregation

- Centralized logging for all services
- Real-time log viewing and search
- Structured logging support

### Prometheus - Metrics Collection

- Collects metrics from the API
- Stores time-series data
- Provides query interface

### Grafana - Metrics Visualization

- Pre-configured dashboards
- Real-time metrics visualization
- Alerting capabilities

## 🛠️ Development Workflow

1. **Start Development Environment**:

   ```bash
   docker-compose -f docker-compose.dev.yml up -d
   ```

2. **Make Code Changes**:

   - Edit files in `./Sql.Baseline.Api/`
   - Changes are automatically detected and reloaded

3. **View Logs**:

   ```bash
   docker-compose -f docker-compose.dev.yml logs -f api
   ```

4. **Stop Development**:
   ```bash
   docker-compose -f docker-compose.dev.yml down
   ```

## 🚀 Production Deployment

1. **Start Production Environment**:

   ```bash
   docker-compose up -d
   ```

2. **Monitor Services**:

   ```bash
   docker-compose ps
   docker-compose logs -f
   ```

3. **Access Monitoring**:

   - Grafana: `http://localhost:3000` (admin/admin)
   - Prometheus: `http://localhost:9090`
   - Seq: `http://localhost:5341`

4. **Stop Production**:
   ```bash
   docker-compose down
   ```

## 🔧 Configuration

### Environment Variables

Both configurations use the same environment variables for consistency:

- `ASPNETCORE_ENVIRONMENT`: Development/Production
- `ConnectionStrings__Default`: PostgreSQL connection string
- `Redis__ConnectionString`: Redis connection string
- `Jwt__*`: JWT configuration

### Volumes

- **Development**: Source code mounted for hot reload
- **Production**: Persistent data volumes for all services

## 🐛 Troubleshooting

### Common Issues

1. **Port Conflicts**:

   - Ensure ports 8080, 5432, 6379 are available
   - Production also uses 3000, 5341, 9090

2. **Database Connection Issues**:

   - Wait for PostgreSQL health check to pass
   - Check connection string format

3. **API Not Starting**:

   - Check logs: `docker-compose logs api`
   - Ensure all dependencies are healthy

4. **Permission Issues**:
   - Development uses PostgreSQL (no permission issues)
   - Production includes proper volume permissions

### Useful Commands

```bash
# View all logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f api

# Check service status
docker-compose ps

# Restart specific service
docker-compose restart api

# Clean up everything
docker-compose down -v --remove-orphans
```

## 🔧 Build Script (`build.ps1`)

The project includes a PowerShell build script that automates Docker Compose operations:

### Features:

- **Automated Environment Setup** - One-command deployment
- **Build Optimization** - Enables Docker BuildKit for faster builds
- **Environment Management** - Clean, rebuild, and manage containers
- **User-Friendly Interface** - Colored output and helpful commands

### Script Modes:

- `dev` - Build and start development environment
- `prod` - Build and start production environment
- `clean` - Clean all containers, images, and volumes
- `rebuild` - Clean everything and rebuild production

### Options:

- `-NoCache` - Force rebuild without using cache
- `-Parallel` - Build services in parallel for speed

## 📝 Notes

- Both configurations use PostgreSQL for consistency
- Development includes hot reload for faster iteration
- Production includes complete monitoring stack
- All services have health checks for reliable startup
- Volumes persist data between container restarts
- Build script provides automated workflow management
