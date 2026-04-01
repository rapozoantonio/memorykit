using Xunit;
using MemoryKit.Application.Services;
using MemoryKit.Domain.Enums;

namespace MemoryKit.Application.Tests.Services;

public class HeuristicFactExtractorTests
{
    #region Person Extraction Tests

    [Fact]
    public void Extract_MyNameIs_ShouldExtractPerson()
    {
        // Arrange
        var text = "Hi, my name is Alice Johnson.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text);

        // Assert
        Assert.Single(entities);
        Assert.Equal("Person", entities[0].Key);
        Assert.Equal("Alice Johnson", entities[0].Value);
        Assert.Equal(EntityType.Person, entities[0].Type);
        Assert.Equal(0.75, entities[0].Importance);
        Assert.True(entities[0].IsNovel);
        Assert.NotNull(entities[0].Embedding);
        Assert.Equal(384, entities[0].Embedding.Length);
    }

    [Fact]
    public void Extract_IAmA_ShouldExtractPerson()
    {
        // Arrange
        var text = "I am a software engineer working on MemoryKit.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text);

        // Assert
        Assert.Single(entities);
        Assert.Equal("Person", entities[0].Key);
        Assert.Contains("software engineer", entities[0].Value, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EntityType.Person, entities[0].Type);
    }

    [Fact]
    public void Extract_IWorkAs_ShouldExtractPerson()
    {
        // Arrange
        var text = "I work as a Senior DevOps Engineer at the company.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text);

        // Assert
        Assert.Single(entities);
        Assert.Equal("Person", entities[0].Key);
        Assert.Contains("DevOps Engineer", entities[0].Value, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EntityType.Person, entities[0].Type);
    }

    #endregion

    #region Technology Extraction Tests

    [Fact]
    public void Extract_UsingTechnology_ShouldExtractTechnology()
    {
        // Arrange
        var text = "We're using PostgreSQL for storage.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text);

        // Assert
        Assert.Single(entities);
        Assert.Equal("Technology", entities[0].Key);
        Assert.Equal("PostgreSQL", entities[0].Value);
        Assert.Equal(EntityType.Technology, entities[0].Type);
        Assert.Equal(0.70, entities[0].Importance);
    }

    [Fact]
    public void Extract_BuiltWith_ShouldExtractTechnology()
    {
        // Arrange
        var text = "The system is built with ASP.NET Core.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text);

        // Assert
        Assert.Single(entities);
        Assert.Equal("Technology", entities[0].Key);
        Assert.Contains("ASP.NET Core", entities[0].Value);
        Assert.Equal(EntityType.Technology, entities[0].Type);
    }

    [Fact]
    public void Extract_PoweredBy_ShouldExtractTechnology()
    {
        // Arrange
        var text = "This is powered by Azure OpenAI.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text);

        // Assert
        Assert.Single(entities);
        Assert.Equal("Technology", entities[0].Key);
        Assert.Contains("Azure OpenAI", entities[0].Value);
        Assert.Equal(EntityType.Technology, entities[0].Type);
    }

    #endregion

    #region Preference Extraction Tests

    [Fact]
    public void Extract_IPrefer_ShouldExtractPreference()
    {
        // Arrange
        var text = "I prefer tabs over spaces when coding.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text);

        // Assert
        Assert.Single(entities);
        Assert.Equal("Preference", entities[0].Key);
        Assert.Contains("tabs", entities[0].Value, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EntityType.Preference, entities[0].Type);
        Assert.Equal(0.60, entities[0].Importance);
    }

    [Fact]
    public void Extract_ILike_ShouldExtractPreference()
    {
        // Arrange
        var text = "I like to write code in the morning.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text);

        // Assert
        Assert.Single(entities);
        Assert.Equal("Preference", entities[0].Key);
        Assert.Contains("write code", entities[0].Value, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EntityType.Preference, entities[0].Type);
    }

    #endregion

    #region Decision Extraction Tests

    [Fact]
    public void Extract_IDecidedTo_ShouldExtractDecision()
    {
        // Arrange
        var text = "I decided to migrate to PostgreSQL.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text);

        // Assert
        Assert.Single(entities);
        Assert.Equal("Decision", entities[0].Key);
        Assert.Contains("migrate", entities[0].Value, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EntityType.Decision, entities[0].Type);
        Assert.Equal(0.85, entities[0].Importance);
    }

    [Fact]
    public void Extract_WeWill_ShouldExtractDecision()
    {
        // Arrange
        var text = "We will implement caching next sprint.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text);

        // Assert
        Assert.Single(entities);
        Assert.Equal("Decision", entities[0].Key);
        Assert.Contains("implement caching", entities[0].Value, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EntityType.Decision, entities[0].Type);
    }

    #endregion

    #region Goal Extraction Tests

    [Fact]
    public void Extract_IWantTo_ShouldExtractGoal()
    {
        // Arrange
        var text = "I want to improve query performance by 50%.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text);

        // Assert
        Assert.Single(entities);
        Assert.Equal("Goal", entities[0].Key);
        Assert.Contains("improve query performance", entities[0].Value, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EntityType.Goal, entities[0].Type);
        Assert.Equal(0.80, entities[0].Importance);
    }

    [Fact]
    public void Extract_PlanningTo_ShouldExtractGoal()
    {
        // Arrange
        var text = "We're planning to add multi-tenancy.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text);

        // Assert
        Assert.Single(entities);
        Assert.Equal("Goal", entities[0].Key);
        Assert.Contains("add multi-tenancy", entities[0].Value, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EntityType.Goal, entities[0].Type);
    }

    #endregion

    #region Constraint Extraction Tests

    [Fact]
    public void Extract_Must_ShouldExtractConstraint()
    {
        // Arrange
        var text = "We must support GDPR compliance.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text);

        // Assert
        Assert.Single(entities);
        Assert.Equal("Constraint", entities[0].Key);
        Assert.Contains("support GDPR", entities[0].Value, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EntityType.Constraint, entities[0].Type);
        Assert.Equal(0.75, entities[0].Importance);
    }

    [Fact]
    public void Extract_Cannot_ShouldExtractConstraint()
    {
        // Arrange
        var text = "We cannot exceed 100ms latency.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text);

        // Assert
        Assert.Single(entities);
        Assert.Equal("Constraint", entities[0].Key);
        Assert.Contains("exceed", entities[0].Value, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EntityType.Constraint, entities[0].Type);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Extract_NullInput_ShouldReturnEmpty()
    {
        // Act
        var entities = HeuristicFactExtractor.Extract(null!);

        // Assert
        Assert.NotNull(entities);
        Assert.Empty(entities);
    }

    [Fact]
    public void Extract_EmptyInput_ShouldReturnEmpty()
    {
        // Act
        var entities = HeuristicFactExtractor.Extract("");

        // Assert
        Assert.NotNull(entities);
        Assert.Empty(entities);
    }

    [Fact]
    public void Extract_VeryLongMessage_ShouldTruncateAndNotCrash()
    {
        // Arrange
        var text = new string('a', 20_000) + " I prefer Redis.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text);

        // Assert - Should not throw, truncation at 10KB means preference might not be extracted
        Assert.NotNull(entities);
    }

    [Fact]
    public void Extract_DuplicateEntities_ShouldDeduplicate()
    {
        // Arrange
        var text = "I prefer Python. I really prefer Python for scripting.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text);

        // Assert - Should only have 1 "Python" preference (deduplicated)
        var pythonPrefs = entities.Where(e =>
            e.Type == EntityType.Preference &&
            e.Value.Contains("Python", StringComparison.OrdinalIgnoreCase)
        ).ToList();
        Assert.Single(pythonPrefs);
    }

    #endregion

    #region Stop Word Removal Tests

    [Fact]
    public void Extract_LeadingStopWords_ShouldRemove()
    {
        // Arrange
        var text = "I work as a the Senior Software Engineer.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text);

        // Assert
        Assert.Single(entities);
        // "a the" should be trimmed from the beginning
        Assert.DoesNotContain("a the", entities[0].Value, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Senior", entities[0].Value);
    }

    [Fact]
    public void Extract_MultiplePatterns_ShouldExtractAll()
    {
        // Arrange
        var text = "My name is Bob. I prefer Redis. We decided to use Docker.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text);

        // Assert
        Assert.Equal(3, entities.Count);
        Assert.Contains(entities, e => e.Type == EntityType.Person);
        Assert.Contains(entities, e => e.Type == EntityType.Preference);
        Assert.Contains(entities, e => e.Type == EntityType.Decision);
    }

    #endregion

    #region Additional Comprehensive Tests

    [Fact]
    public void Extract_ComplexMessage_ShouldExtractMultipleTypes()
    {
        // Arrange
        var text = @"
            Hi, my name is Sarah Chen. I work as a Principal Engineer at TechCorp.
            We're using Kubernetes for container orchestration.
            I prefer functional programming over object-oriented.
            We decided to migrate our microservices to AWS.
            I want to improve our system reliability by implementing chaos engineering.
            We must maintain 99.9% uptime for our services.
        ";

        // Act
        var entities = HeuristicFactExtractor.Extract(text);

        // Assert
        Assert.True(entities.Count >= 6, "Should extract at least 6 different entities");
        Assert.Contains(entities, e => e.Type == EntityType.Person);
        Assert.Contains(entities, e => e.Type == EntityType.Technology);
        Assert.Contains(entities, e => e.Type == EntityType.Preference);
        Assert.Contains(entities, e => e.Type == EntityType.Decision);
        Assert.Contains(entities, e => e.Type == EntityType.Goal);
        Assert.Contains(entities, e => e.Type == EntityType.Constraint);
    }

    [Fact]
    public void Extract_WhitespaceOnly_ShouldReturnEmpty()
    {
        // Arrange
        var text = "   \t\n\r   ";

        // Act
        var entities = HeuristicFactExtractor.Extract(text);

        // Assert
        Assert.Empty(entities);
    }

    [Fact]
    public void Extract_SpecialCharacters_ShouldNotCrash()
    {
        // Arrange
        var text = "I prefer 🐍 Python and ☕ Java! We're using C#.NET Core!!!";

        // Act
        var entities = HeuristicFactExtractor.Extract(text);

        // Assert - Should extract technology despite emojis
        Assert.NotNull(entities);
        Assert.Contains(entities, e => e.Type == EntityType.Technology);
    }

    #endregion

    #region Narrative Fallback Tests

    [Fact]
    public void Extract_BiographicalNarrative_ShouldExtractNarrativeFragments()
    {
        // Arrange
        var text = "My first time riding a bicycle I was 7 years old. It was in the playground of the first apartment I lived in, in Niterói.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text, useNarrativeFallback: true);

        // Assert - Should extract narrative memory, age, and potentially location
        Assert.NotEmpty(entities);
        
        // Should have Memory entity
        var memoryEntity = entities.FirstOrDefault(e => e.Key == "Memory");
        Assert.NotNull(memoryEntity);
        Assert.Equal(EntityType.Other, memoryEntity.Type);
        Assert.Equal(0.50, memoryEntity.Importance);
        
        // Should have Age entity
        var ageEntity = entities.FirstOrDefault(e => e.Key == "Age");
        Assert.NotNull(ageEntity);
        Assert.Contains("7 years old", ageEntity.Value);
        
        // Should have at least one Location entity (playground or Niterói)
        var locationEntities = entities.Where(e => e.Type == EntityType.Place).ToList();
        Assert.NotEmpty(locationEntities);
        Assert.All(locationEntities, e => Assert.Equal(0.55, e.Importance));
    }

    [Fact]
    public void Extract_NarrativeFallback_ShouldExtractAge()
    {
        // Arrange
        var text = "When I was 25 years old, I started my first company.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text, useNarrativeFallback: true);

        // Assert
        Assert.NotEmpty(entities);
        var ageEntity = entities.FirstOrDefault(e => e.Key == "Age");
        Assert.NotNull(ageEntity);
        Assert.Contains("25 years old", ageEntity.Value);
        Assert.Equal(EntityType.Other, ageEntity.Type);
    }

    [Fact]
    public void Extract_NarrativeFallback_ShouldExtractPlaceAsEntityTypePlace()
    {
        // Arrange
        var text = "I grew up in Barcelona and later moved to Tokyo.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text, useNarrativeFallback: true);

        // Assert
        Assert.NotEmpty(entities);
        var locationEntities = entities.Where(e => e.Type == EntityType.Place).ToList();
        Assert.NotEmpty(locationEntities);
        Assert.Contains(locationEntities, e => e.Value.Contains("Barcelona"));
        Assert.All(locationEntities, e => Assert.Equal(0.55, e.Importance));
    }

    [Fact]
    public void Extract_NarrativeFallback_ShouldLimitToMaxFragments()
    {
        // Arrange
        var text = "First sentence about something. Second sentence about another thing. Third sentence here. Fourth sentence. Fifth sentence.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text, useNarrativeFallback: true, maxFragments: 2);

        // Assert
        var memoryEntities = entities.Where(e => e.Key == "Memory").ToList();
        Assert.True(memoryEntities.Count <= 2, $"Expected at most 2 memory fragments, got {memoryEntities.Count}");
    }

    [Fact]
    public void Extract_NarrativeFallback_DisabledWhenConfigIsFalse()
    {
        // Arrange
        var text = "This is a random narrative with no structured facts.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text, useNarrativeFallback: false);

        // Assert - Should return empty when narrative fallback is disabled
        Assert.Empty(entities);
    }

    [Fact]
    public void Extract_NarrativeFallback_NotTriggeredWhenStructuredFactsExist()
    {
        // Arrange
        var text = "I prefer Python for backend development.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text, useNarrativeFallback: true);

        // Assert - Should extract Preference, not trigger narrative fallback
        Assert.Single(entities);
        Assert.Equal("Preference", entities[0].Key);
        Assert.Equal(EntityType.Preference, entities[0].Type);
        Assert.DoesNotContain(entities, e => e.Key == "Memory");
    }

    [Fact]
    public void Extract_ExperiencePattern_ShouldExtractLifeEvents()
    {
        // Arrange
        var text = "I learned to code when I was young. My first time programming was amazing.";

        // Act
        var entities = HeuristicFactExtractor.Extract(text, useNarrativeFallback: true);

        // Assert
        Assert.NotEmpty(entities);
        var experienceEntity = entities.FirstOrDefault(e => e.Key == "Experience");
        Assert.NotNull(experienceEntity);
        Assert.Contains("code", experienceEntity.Value, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Extract_CustomImportanceScore_ShouldApplyToNarrativeFragments()
    {
        // Arrange
        var text = "This is a narrative story about my childhood.";
        var customImportance = 0.65;

        // Act
        var entities = HeuristicFactExtractor.Extract(
            text,
            useNarrativeFallback: true,
            narrativeImportance: customImportance);

        // Assert
        Assert.NotEmpty(entities);
        var memoryEntity = entities.FirstOrDefault(e => e.Key == "Memory");
        Assert.NotNull(memoryEntity);
        Assert.Equal(customImportance, memoryEntity.Importance);
    }

    #endregion
}
