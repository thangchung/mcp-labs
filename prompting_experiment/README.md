# Get starting with aichatweb

```sh
dotnet new install Microsoft.Extensions.AI.Templates
```

```sh
dotnet new aichatweb --provider azureopenai --vector-store qdrant --managed-identity false --aspire true -C gpt-4o-mini -E text-embedding-3-small 
```

## References

This is all references to the project.

- https://spring.io/blog/2025/04/14/spring-ai-prompt-engineering-patterns
- https://learn.microsoft.com/en-us/dotnet/ai/conceptual/prompt-engineering-dotnet
- See ChatApp: https://devblogs.microsoft.com/dotnet/announcing-dotnet-ai-template-preview2/#🛠-install-the-updated-template