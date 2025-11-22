# SceneSplit

SceneSplit is a distributed, event-driven image analysis platform. Its primary goal is to accept a general “scene” image upload, compress it efficiently, invoke AI to detect objects contained within that scene, and then automatically search the internet for representative images of each detected object. The system stitches together multiple technologies (ASP.NET Core APIs, gRPC-Web services, AWS Lambdas, Amazon Bedrock, S3, SQS, SignalR, Angular) to demonstrate modern polyglot service design and scalable cloud-native workflow orchestration.

## High-Level Architecture

![High-Level Architecture](HLA.png)

### Data / Workflow Overview
1. User uploads a scene image via the Angular frontend to `SceneSplit.Api` (ASP.NET Core) over HTTP/SignalR.
2. API validates metadata, invokes the gRPC compression service (`SceneSplit.ImageCompression.Api`) to resize/compress the original image for storage efficiency.
3. Compressed image is stored in S3 (`scene-split-scene-images`) tagged with workflow identifiers (`WorkflowTags.USER_ID_TAG`, `WorkflowTags.WORKFLOW_ID`).
4. An S3 event triggers `SceneSplit.SceneAnalysisLambda`, which downloads the image and calls Amazon Bedrock (via `Microsoft.Extensions.AI`) to perform object detection / scene analysis.
5. Lambda publishes a `SceneAnalysisResult` message to the SQS queue `scene-split-detected-objects` containing the list of detected objects and workflow context.
6. `SceneSplit.ObjectImageSearchLambda` (SQS-triggered) consumes the message, queries an external image search API for each object, downloads found images, compresses them via gRPC again, and uploads processed object images back to S3 with the original workflow tagging.
7. API (via polling, subscription, or hub-driven notifications) relays progress/results to the user’s SignalR group identified by userId, enabling near real-time UX updates.

## Technology Stack

| Layer | Tech | Purpose |
|-------|------|---------|
| Frontend | Angular 18 + Tailwind | Upload UX, real-time status via SignalR hub `/api/hubs/scene-split`. |
| API Service | ASP.NET Core (.NET 8) | HTTP + SignalR, CQRS (MediatR), Orchestrates compression & storage. Path base `/api`. |
| Compression Service | ASP.NET Core gRPC-Web | Large image compression (resizing/quality). Exposed via gRPC-Web for browser compatibility. |
| AI Integration | Amazon Bedrock + `Microsoft.Extensions.AI` | Object detection / scene analysis. |
| Message Bus | Amazon SQS | Decouples object detection results from image search processing. |
| Object Search Lambda | .NET 8 AWS Lambda | Internet image search for detected objects; secondary compression & storage. |
| Scene Analysis Lambda | .NET 8 AWS Lambda | S3-triggered AI inference and result publication. |
| Storage | Amazon S3 | Durable storage for original + compressed scene and object images. |
| Infra-as-Code | AWS CDK (C#) | Provisions VPC, Fargate services (APIs/frontends), S3, SQS, Lambdas, ALBs, API Gateway. |
| Realtime | SignalR | User-specific groups for workflow progress broadcasting. |

## Services Breakdown

### `SceneSplit.Api`
Responsible for accepting uploads, invoking compression, tagging workflow assets, and pushing progress to clients. Utilizes CQRS (MediatR commands & queries), with a Hub at `/api/hubs/scene-split` that groups connections by `userId`.

### `SceneSplit.ImageCompression.Api`
gRPC-Web service providing high-throughput image compression, enforcing maximum sizes defined by configuration (`MAX_IMAGE_SIZE`). Shared client factory (`GrpcClientFactoryHelper`) standardizes channel creation and send/receive limits.

### `SceneSplit.SceneAnalysisLambda`
Triggered by S3 object creation events. Downloads the scene image, invokes Bedrock via `Microsoft.Extensions.AI` with a configured model (`BEDROCK_MODEL_ID`), limits results (`MAX_ITEMS`), and emits `SceneAnalysisResult` to SQS (`SQS_QUEUE_URL`).

### `SceneSplit.ObjectImageSearchLambda`
Triggered by SQS messages. For each detected object: calls external image search API (`IMAGE_SEARCH_API_ENDPOINT`), authenticates with `IMAGE_SEARCH_API_KEY`, compresses results through compression gRPC (`COMPRESSION_API_URL`), and stores them in S3 with original workflow tags. Supports configuration for dimensions and quality (`RESIZE_WIDTH`, `RESIZE_HEIGHT`, `IMAGE_QUALITY_COMPRESSION`).

### Shared Packages
- `SceneSplit.Configuration`: Centralized configuration key constants used by services and infra.
- `SceneSplit.GrpcClientShared`: Utilities for constructing gRPC clients with size limits.
- `SceneSplit.LambdaShared`: Common Lambda utilities (serialization, logging harnesses).
- `SceneSplit.SceneAnalysisLambda.Sdk` / others: DTOs & contracts reused across services/tests.

## Infrastructure (AWS CDK - C#)
Located in `infra/SceneSplit.Cdk`. The stack provisions:
- VPC + Security Groups for isolation.
- ECS/Fargate services: API, Compression API, Angular frontend (NGINX container).
- Application Load Balancers (ALBs) with health checks (curl-based endpoints).
- Public API Gateway that fronts the internal API ALB (`/{proxy+}` pass-through).
- S3 bucket `scene-split-scene-images` for all workflow images.
- SQS queue `scene-split-detected-objects` for analysis->search decoupling.
- Lambda wiring (S3 event source, SQS event source). Environment variables mapped using configuration keys.

### Deployment Footprint
Artifacts produced by CI pipeline: `frontend-build`, `backend-publish`, `backend-lambda-publish` (zip bundles under `lambda-publish/`). CDK synth runs on PRs; deploy requires proper AWS credentials & bootstrap.

## Local Development

### Prerequisites

* **.NET 8 SDK**
* **Node 20+** and **Angular CLI 18**
* **AWS credentials** (for deploying real infrastructure)
  or local mocks if introduced later
* *(Optional)* **AWS CDK CLI** — useful for `cdk synth` or manual deploys

---

## Build & Test

### Backend

```pwsh
# Build backend solution
dotnet build src/SceneSplit.Backend/SceneSplit.Backend.sln -c Release

# Run all backend tests with coverage
dotnet test tests/*/*.csproj -c Release --settings coverage.runsettings
```

### Frontend

```pwsh
cd src/SceneSplit.Frontend
npm install --force
ng test
```

---

## Running Services Locally (Aspire)

```pwsh
# 1. Start the Aspire AppHost (provisions local S3/SQS equivalents)
dotnet run --project src/SceneSplit.Backend/SceneSplit.Backend.AppHost

# 2. Start the Angular frontend
cd src/SceneSplit.Frontend
ng s
```

> Lambda event wiring (S3/SQS triggers) is not available locally.
> Use the **Lambda Test Tool** in the Aspire dashboard to invoke Lambda functions during development.

---

## Running Services in AWS (CDK)

```pwsh
cdk deploy
```

This deploys the full backend stack (S3, SQS, Lambdas, APIs, etc.) to your configured AWS account.

## Configuration Keys
All environment variable names are centralized (see `SceneSplit.Configuration/ConfigurationKeys.cs`). Use these constants in code and when binding Lambda / ECS task definitions to minimize drift and typos.

## Testing & Quality
- Unit tests per project under `tests/` (mirrors CI). Logging validated by checking `EventId.Name` matches static `Log` helper names.
- Coverage generated in Cobertura format; optional HTML via `reportgenerator` tool.
- Deterministic log messages facilitate reliable assertions.

## Real-Time Updates
SignalR hub groups clients by userId. Workflow progress (upload accepted, compression complete, analysis started, objects detected, search results posted) can be pushed incrementally. Large payload handling requires configuring MaxSend/Receive sizes consistently across SignalR and gRPC clients to accommodate big images.

## Design Principles
- Event-driven decoupling via S3 + SQS.
- Explicit workflow tagging in S3 objects for traceability & multi-user isolation.
- Configuration-as-code through shared constants.
- Size-conscious processing (compression early & often).
- Observable, testable logging with strongly-typed event IDs.

## CI/CD Summary
GitHub Actions pipeline (`.github/workflows/pipeline.yml`):
- Node 20 & .NET 8 setup.
- Build + unit tests with coverage & persistent PR comments.
- Artifact publishing (frontend build, backend publish outputs, lambda zips).
- `cdk synth` validation on pull requests; manual deploy step gated by credentials.