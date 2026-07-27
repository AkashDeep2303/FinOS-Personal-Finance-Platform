using FinOS.Common.Exceptions;
using FinOS.Investment.Application.DTOs;
using FinOS.Investment.Domain.Interfaces;
using MediatR;

namespace FinOS.Investment.Application.Commands;

public record CreateSIPCommand(long UserId, CreateSIPRequest Request) : IRequest<SIPDto>;
public record UpdateSIPCommand(long UserId, long Id, UpdateSIPRequest Request) : IRequest<SIPDto>;
public record ChangeSIPStatusCommand(long UserId, long Id, bool IsActive) : IRequest;
public record DeleteSIPCommand(long UserId, long Id) : IRequest;

public class CreateSIPCommandHandler : IRequestHandler<CreateSIPCommand, SIPDto>
{
    private readonly ISIPRepository _repo;
    public CreateSIPCommandHandler(ISIPRepository repo) => _repo = repo;
    public async Task<SIPDto> Handle(CreateSIPCommand c, CancellationToken ct)
    {
        var r = c.Request;
        var id = await _repo.CreateAsync(c.UserId, r.FundName, r.HoldingId, r.MonthlyAmount, r.Frequency.ToString(), r.DayOfMonth, r.StartDate, r.EndDate, r.SourceAccountId, ct);
        return (await _repo.GetByIdAsync(id, ct) is { } sip) ? SIPMapper.Map(sip) : throw new NotFoundException("SIP", id);
    }
}

public class UpdateSIPCommandHandler : IRequestHandler<UpdateSIPCommand, SIPDto>
{
    private readonly ISIPRepository _repo;
    public UpdateSIPCommandHandler(ISIPRepository repo) => _repo = repo;
    public async Task<SIPDto> Handle(UpdateSIPCommand c, CancellationToken ct)
    {
        var r = c.Request;
        await _repo.UpdateAsync(c.Id, c.UserId, r.FundName, r.HoldingId, r.MonthlyAmount, r.Frequency.ToString(), r.DayOfMonth, r.StartDate, r.EndDate, r.SourceAccountId, ct);
        return (await _repo.GetByIdAsync(c.Id, ct) is { } sip) ? SIPMapper.Map(sip) : throw new NotFoundException("SIP", c.Id);
    }
}

public class ChangeSIPStatusCommandHandler : IRequestHandler<ChangeSIPStatusCommand>
{
    private readonly ISIPRepository _repo;
    public ChangeSIPStatusCommandHandler(ISIPRepository repo) => _repo = repo;
    public async Task Handle(ChangeSIPStatusCommand c, CancellationToken ct) => await _repo.SetStatusAsync(c.Id, c.UserId, c.IsActive, ct);
}

public class DeleteSIPCommandHandler : IRequestHandler<DeleteSIPCommand>
{
    private readonly ISIPRepository _repo;
    public DeleteSIPCommandHandler(ISIPRepository repo) => _repo = repo;
    public async Task Handle(DeleteSIPCommand c, CancellationToken ct) => await _repo.DeleteAsync(c.Id, c.UserId, ct);
}

internal static class SIPMapper
{
    public static SIPDto Map(Domain.Entities.SIP s) => new()
    {
        Id = s.Id, UserId = s.UserId, HoldingId = s.HoldingId, FundName = string.IsNullOrWhiteSpace(s.Name) ? s.Holding?.Name ?? "SIP" : s.Name,
        MonthlyAmount = s.Amount, CurrentValue = s.Holding?.CurrentValue ?? s.TotalInvested, Frequency = s.Frequency,
        DayOfMonth = s.DayOfMonth, StartDate = s.StartDate, EndDate = s.EndDate, NextExecutionDate = s.NextExecutionDate,
        IsActive = s.IsActive, TotalInvested = s.TotalInvested, InstallmentsDone = s.InstallmentsDone, SourceAccountId = s.SourceAccountId ?? 0
    };
}
