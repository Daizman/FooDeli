# FooDeli

A reference microservices project demonstrating modern .NET practices for building distributed systems.

## About

This is an educational project designed for .NET developers who have completed a foundational course and want to explore advanced topics before job interviews.

**Not production-ready** — architecture decisions prioritize learning over optimization.

## Covered Topics

- **Data Access**: Dapper, Repository pattern, transactions, Unit of Work
- **Microservices Communication**: REST, gRPC, Apache Kafka
- **Event Sourcing & CQRS**: Marten on PostgreSQL
- **Authentication**: Keycloak, S2S tokens (Client Credentials), JWT validation
- **API Gateway**: YARP (reverse proxy, routing)
- **Containerization**: Docker, Docker Compose
- **Observability**: Serilog, OpenTelemetry, Prometheus
- **Testing**: Integration tests with Testcontainers
- **CI/CD**: GitHub Actions

## Architecture

```
                              ┌──────────────┐
                              │   Clients    │
                              └──────┬───────┘
                                     │
                                     ▼
                              ┌──────────────┐
                              │ API Gateway  │
                              │    (YARP)    │
                              └──────┬───────┘
                                     │
         ┌───────────────┬───────────┼───────────┬───────────────┐
         ▼               ▼           ▼           ▼               ▼
   ┌───────────┐   ┌───────────┐ ┌───────────┐ ┌───────────┐ ┌───────────┐
   │Restaurant │   │   Order   │ │  Kitchen  │ │ Delivery  │ │  Notifi-  │
   │  Service  │◄─►│  Service  │ │  Service  │ │  Service  │ │  cation   │
   │           │   │           │ │           │ │           │ │  Service  │
   │  [gRPC]   │   │[ES/CQRS]  │ │  [Kafka]  │ │  [REST]   │ │  [Kafka]  │
   └─────┬─────┘   └─────┬─────┘ └─────┬─────┘ └─────┬─────┘ └───────────┘
         │               │             │             │
         │               ▼             │             │
         │         ┌───────────┐       │             │
         │         │   Kafka   │◄──────┴─────────────┘
         │         └───────────┘
         │               │
         ▼               ▼
   ┌─────────────────────────────────────┐       ┌───────────┐
   │            PostgreSQL               │       │ Keycloak  │
   └─────────────────────────────────────┘       └───────────┘
```

## Services

| Service | Port | Description | Communication |
|---------|------|-------------|---------------|
| API Gateway | 5000 | Routing, auth | REST in, REST/gRPC out |
| Restaurant | 5010 | Restaurants, menus | REST, gRPC server |
| Order | 5020 | Orders, ES/CQRS | REST, gRPC client, Kafka producer |
| Kitchen | 5030 | Cooking queue | Kafka consumer/producer |
| Delivery | 5040 | Couriers, tracking | REST, Kafka consumer/producer |
| Notification | 5050 | Email, push (stubs) | Kafka consumer |

## Tech Stack

| Category | Technology |
|----------|------------|
| Runtime | .NET 10 |
| Data Access | Dapper, Marten |
| Database | PostgreSQL 16 |
| Messaging | Apache Kafka |
| RPC | gRPC |
| Gateway | YARP |
| Auth | Keycloak 24 |
| Observability | Serilog, OpenTelemetry, Prometheus |
| Testing | xUnit, Testcontainers |
| Containers | Docker, Docker Compose |

## Quick Start

### Prerequisites

- Docker & Docker Compose
- .NET 10 SDK
- IDE (Rider, VS Code, Visual Studio)

### Run Infrastructure

```bash
docker-compose up -d
```

### Access Points

| Service | URL |
|---------|-----|
| API Gateway | http://localhost:5000 |
| Keycloak Admin | http://localhost:8080 (admin/admin) |
| Prometheus | http://localhost:9090 |

### Get Access Token (Postman/curl)

```bash
curl -X POST http://localhost:8080/realms/food-delivery/protocol/openid-connect/token \
  -d "client_id=food-delivery-app" \
  -d "username=customer1" \
  -d "password=customer1" \
  -d "grant_type=password"
```

## Repository Structure

```
food-delivery/
├── src/
│   ├── ApiGateway/                 # YARP gateway
│   ├── Services/
│   │   ├── Restaurant/
│   │   │   ├── Restaurant.API/
│   │   │   ├── Restaurant.Domain/
│   │   │   ├── Restaurant.Infrastructure/
│   │   │   └── Restaurant.IntegrationTests/
│   │   ├── Order/                  # ES/CQRS with Marten
│   │   ├── Kitchen/
│   │   ├── Delivery/
│   │   └── Notification/
│   └── Shared/
│       ├── Shared.Contracts/       # gRPC protos, shared DTOs
│       ├── Shared.Auth/            # Keycloak integration
│       └── Shared.Messaging/       # Kafka abstractions
├── infrastructure/
│   ├── keycloak/
│   │   └── realm-export.json
│   └── kafka/
│       └── init-topics.sh
├── docs/
│   ├── event-sourcing.md
│   ├── kafka-patterns.md
│   └── grpc-vs-rest.md
├── docker-compose.yml
└── README.md
```

## Main Flows

### 1. Create Order

```
Client → Gateway → Order Service
                        │
                        ├──► Restaurant Service (gRPC): validate items, get prices
                        │
                        └──► Kafka [order.created]
                                    │
                        ┌───────────┴───────────┐
                        ▼                       ▼
                   Kitchen Service      Notification Service
                   (start cooking)         (send confirmation)
```

### 2. Order Lifecycle

```
[order.created] → Kitchen picks up
                       │
                       ▼
              [order.ready] → Delivery assigns courier
                                    │
                                    ▼
                          [delivery.picked_up]
                                    │
                                    ▼
                          [delivery.completed] → Notification
```

## Documentation

- [Event Sourcing & CQRS](docs/event-sourcing.md)
- [Kafka Patterns](docs/kafka-patterns.md)
- [gRPC vs REST: When to Use](docs/grpc-vs-rest.md)
- [Integration Testing](docs/testing.md)
