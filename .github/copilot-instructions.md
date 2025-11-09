## SceneSplit — AI agent working notes

This repo is a multi-service app with AWS CDK infra, a .NET 8 backend (APIs + Lambdas), and an Angular 18 frontend. Use these notes to navigate the architecture, patterns, and dev workflows quickly.

### Architecture at a glance
- Infra (C# CDK) in `infra/SceneSplit.Cdk` provisions:
  - VPC, ECS/Fargate services for: `SceneSplit.Api` (HTTP, SignalR), `SceneSplit.ImageCompression.Api` (gRPC-Web), and the public frontend (NGINX).
  - S3 bucket `scene-split-scene-images` and SQS queue `scene-split-detected-objects`.
  - Lambda: `SceneSplit.SceneAnalysisLambda` (S3 -> Bedrock -> SQS). See `Constructs/*.cs` for wiring (ALBs, health checks via curl, security groups).
  - API Gateway fronts the internal API ALB and routes `/{proxy+}` to the API service.

### Backend services and data flow
- `SceneSplit.Api` (ASP.NET Core):
  - Path base `/api`; CORS from `ApiConfigurationKeys.ALLOWED_CORS_ORIGINS`.
  - SignalR hub at `/api/hubs/scene-split` (`Hubs/SceneSplitHub.cs`). Hub uses MediatR Commands/Queries to process uploads and push updates to user groups.
  - On upload, images are validated and compressed via gRPC client `Compression.CompressionClient`, then stored to S3 with workflow tags (`WorkflowTags.USER_ID_TAG`, `WorkflowTags.WORKFLOW_ID`). See `Commands/ProcessSceneImage/*` and `Services/StorageService/S3StorageService.cs`.
- `SceneSplit.ImageCompression.Api` (gRPC-Web): exposes `CompressionService` and enables Grpc-Web by default. Clients set MaxSend/Receive sizes based on `MAX_IMAGE_SIZE`.
- Lambdas:
  - `SceneSplit.SceneAnalysisLambda` (S3 event): downloads the image, calls Amazon Bedrock via `Microsoft.Extensions.AI` to detect items, publishes `SceneAnalysisResult` to SQS. Env keys: `SQS_QUEUE_URL`, `MAX_ITEMS`, `BEDROCK_MODEL_ID`.
  - `SceneSplit.ObjectImageSearchLambda` (SQS event): searches external image API, downloads images, calls Compression gRPC, then uploads to S3 with original workflow tags. Env keys: `BUCKET_NAME`, `IMAGE_SEARCH_API_KEY`, `IMAGE_SEARCH_API_ENDPOINT`, `COMPRESSION_API_URL`, `MAX_IMAGE_SIZE`, `RESIZE_WIDTH`, `RESIZE_HEIGHT`, `IMAGE_QUALITY_COMPRESSION`.

### Project conventions you’ll see
- Configuration keys centralized in `src/SceneSplit.Backend/SceneSplit.Configuration/ConfigurationKeys.cs` — reference these constants in code and CDK env vars.
- CQRS with MediatR: commands/queries live under `SceneSplit.Api/Commands/*` and `SceneSplit.Api/Queries/*`.
- SignalR patterns: map clients with `ISceneSplitHubClient`, group by `userId`, and broadcast via `Clients.Group(userId)`. Frontend connects to `/api/hubs/scene-split`.
- gRPC client setup: use `SceneSplit.GrpcClientShared.Helpers/GrpcClientFactoryHelper` and configure MaxSend/Receive sizes to match large images.
- Structured logging: static `Log` classes generate event IDs; tests assert on `EventId.Name == nameof(Log.SomeEvent)`. Prefer logging via these helpers and keep messages deterministic.

### Local build, test, and run
- Requirements: .NET 8 SDK, Node 20+, Angular CLI 18, optional AWS CDK (for synth/deploy).
- Backend build/tests (mirrors CI):
  - Build: `dotnet build src/SceneSplit.Backend/SceneSplit.Backend.sln -c Release`
  - Test all: `dotnet test tests/*/*.csproj -c Release --settings coverage.runsettings`
  - Coverage HTML (optional): install `reportgenerator`, then run with input `TestResults/**/coverage.cobertura.xml`.
- Frontend:
  - Install: `npm install --force` in `src/SceneSplit.Frontend`
  - Unit tests: `npm run test:ci` (headless) or `npm run test`
  - Dev server: `npm start` (ensure backend URL + `/api` base is proxied correctly; hub path is `/api/hubs/scene-split`).
- Running APIs locally:
  - Compression API (`SceneSplit.ImageCompression.Api`) enables gRPC-Web; run it first and set `COMPRESSION_API_URL` for API/Lambda clients.
  - `SceneSplit.Api` expects `SCENE_IMAGE_BUCKET` and other keys; in unit tests these are mocked — for local end-to-end, use real AWS creds/resources or a local emulator.

### CI/CD cues (see `.github/workflows/pipeline.yml`)
- Node 20 and .NET 8, unit tests with Cobertura coverage, sticky PR comments for coverage.
- Artifacts: `frontend-build`, `backend-publish`, and `backend-lambda-publish` (zips under `lambda-publish/`).
- CDK jobs: `cdk synth` on PRs; manual `deploy` step requires AWS creds and bootstrapping.

### When adding or changing features
- New API flows: add MediatR Command/Query + handler, wire via Hub/Controller, and follow the validation and logging style in `ProcessSceneImageCommandHandler`.
- New infra: prefer a dedicated Construct in `infra/SceneSplit.Cdk/Constructs/` and pass configuration via `SceneSplit.Configuration` keys; expose endpoints to services via env vars.
- Large payloads: set MaxSend/Receive sizes for SignalR and gRPC consistently with `MAX_IMAGE_SIZE`.

Key files to browse first:
- `src/SceneSplit.Backend/SceneSplit.Api/Program.cs` and `Hubs/SceneSplitHub.cs`
- `src/SceneSplit.Backend/SceneSplit.ImageCompression.Api/Program.cs`
- `src/SceneSplit.Backend/SceneSplit.SceneAnalysisLambda/Function.cs`
- `infra/SceneSplit.Cdk/SceneSplitStack.cs` and `Constructs/*`

Questions or gaps? Tell us where the workflow was unclear (e.g., local env vars for end-to-end runs), and we’ll refine this doc.
