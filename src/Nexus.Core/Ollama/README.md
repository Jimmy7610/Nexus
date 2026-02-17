OllamaClient responsibilities:
- Detect available local models
- Default to llama3:latest
- Fallback order: mistral -> phi3
- Inject system context (metrics + storage summaries)
- Return structured text recommendations (no auto-actions)
