using FinOS.Investment.Application.DTOs;
using FinOS.Investment.Domain.Interfaces;
using FinOS.Common.Exceptions;
using FinOS.Common.Interfaces;
using MediatR;

namespace FinOS.Investment.Application.Commands;

public class CreatePortfolioCommand : IRequest<PortfolioDto>
{
    public CreatePortfolioRequest Request { get; set; }

    public CreatePortfolioCommand(CreatePortfolioRequest request)
    {
        Request = request;
    }
}

public class CreatePortfolioCommandHandler : IRequestHandler<CreatePortfolioCommand, PortfolioDto>
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePortfolioCommandHandler(IPortfolioRepository portfolioRepository, IUnitOfWork unitOfWork)
    {
        _portfolioRepository = portfolioRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PortfolioDto> Handle(CreatePortfolioCommand command, CancellationToken ct)
    {
        var req = command.Request;

        var portfolio = new Domain.Entities.Portfolio
        {
            UserId = req.UserId,
            Name = req.Name,
            Description = req.Description,
            Currency = req.Currency,
            IsDefault = req.IsDefault
        };

        await _portfolioRepository.AddAsync(portfolio, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new PortfolioDto
        {
            Id = portfolio.Id,
            UserId = portfolio.UserId,
            Name = portfolio.Name,
            Description = portfolio.Description,
            Currency = portfolio.Currency,
            IsDefault = portfolio.IsDefault,
            CreatedAt = portfolio.CreatedAt
        };
    }
}
