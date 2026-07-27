using FinOS.Common.Exceptions;using FinOS.CoreFinance.Domain.Entities;using FinOS.CoreFinance.Domain.Interfaces;using MediatR;namespace FinOS.CoreFinance.Application.Commands;
public record AddAssetCommand(long UserId,Asset Asset):IRequest<Asset>;public record DeleteAssetCommand(long UserId,long Id):IRequest;
public class AddAssetHandler(IAssetRepository r):IRequestHandler<AddAssetCommand,Asset>{public Task<Asset>Handle(AddAssetCommand q,CancellationToken ct){q.Asset.UserId=q.UserId;return r.AddAsync(q.Asset,ct);}}
public class DeleteAssetHandler(IAssetRepository r):IRequestHandler<DeleteAssetCommand>{public async Task Handle(DeleteAssetCommand q,CancellationToken ct){if(!await r.DeleteAsync(q.Id,q.UserId,ct))throw new NotFoundException("Asset",q.Id);}}
