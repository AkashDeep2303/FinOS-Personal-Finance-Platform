using FinOS.AIAssistant.Application.DTOs;
using FinOS.AIAssistant.Application.Services;
using FinOS.AIAssistant.Domain.Entities;
using FinOS.AIAssistant.Domain.Enums;
using FinOS.AIAssistant.Domain.Interfaces;
using FinOS.Common.Exceptions;
using MediatR;

namespace FinOS.AIAssistant.Application.Commands;

public record SendMessageCommand(SendMessageDto Dto) : IRequest<MessageResponseDto>;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, MessageResponseDto>
{
    private readonly IAIConversationRepository _conversationRepository;
    private readonly IAIMessageRepository _messageRepository;
    private readonly ILLMService _llmService;

    public SendMessageCommandHandler(
        IAIConversationRepository conversationRepository,
        IAIMessageRepository messageRepository,
        ILLMService llmService)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _llmService = llmService;
    }

    public async Task<MessageResponseDto> Handle(SendMessageCommand request, CancellationToken ct)
    {
        var dto = request.Dto;

        // Verify conversation exists
        var conversation = await _conversationRepository.GetWithMessagesAsync(dto.ConversationId, ct)
            ?? throw new NotFoundException("Conversation", dto.ConversationId);

        if (conversation.UserId != dto.UserId)
            throw new UnauthorizedAccessException();

        // Build conversation context from last 10 messages
        var recentMessages = conversation.Messages
            .OrderByDescending(m => m.CreatedAt)
            .Take(10)
            .OrderBy(m => m.CreatedAt)
            .ToList();

        var context = string.Join("\n", recentMessages.Select(m => $"{m.Role}: {m.Content}"));

        // Save user message
        var userMessage = new AIMessage
        {
            ConversationId = dto.ConversationId,
            Role = MessageRole.User,
            Content = dto.Content,
            QueryType = dto.QueryType,
            CreatedAt = DateTime.UtcNow
        };

        userMessage = await _messageRepository.CreateAsync(userMessage, ct);

        // Call LLM
        var llmResponse = await _llmService.SendMessageAsync(dto.UserId, dto.Content, dto.QueryType, context, ct);

        // Save assistant message
        var assistantMessage = new AIMessage
        {
            ConversationId = dto.ConversationId,
            Role = MessageRole.Assistant,
            Content = llmResponse.Content,
            QueryType = dto.QueryType,
            TokenCount = llmResponse.TokenCount,
            ResponseTimeMs = llmResponse.ResponseTimeMs,
            CreatedAt = DateTime.UtcNow
        };

        assistantMessage = await _messageRepository.CreateAsync(assistantMessage, ct);

        // Update conversation timestamp
        conversation.UpdatedAt = DateTime.UtcNow;
        await _conversationRepository.UpdateAsync(conversation, ct);

        return new MessageResponseDto(
            new MessageDto(userMessage.Id, userMessage.ConversationId, userMessage.Role,
                userMessage.Content, userMessage.QueryType, userMessage.ReferencedEntityIds,
                userMessage.TokenCount, userMessage.ResponseTimeMs, userMessage.CreatedAt),
            new MessageDto(assistantMessage.Id, assistantMessage.ConversationId, assistantMessage.Role,
                assistantMessage.Content, assistantMessage.QueryType, assistantMessage.ReferencedEntityIds,
                assistantMessage.TokenCount, assistantMessage.ResponseTimeMs, assistantMessage.CreatedAt)
        );
    }
}
