using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Investment.Domain.Entities;
using FinOS.Investment.Domain.Interfaces;

namespace FinOS.Investment.Infrastructure.Repositories;

public class SIPRepository : ISIPRepository
{
    private readonly IConnectionFactory _factory;
    public SIPRepository(IConnectionFactory factory) => _factory = factory;

    private async Task<List<SIP>> QueryAsync(string where, object? args = null)
    {
        using var db = _factory.CreateConnection();
        var sql = $@"SELECT s.*, h.* FROM Investment.SIPs s
LEFT JOIN Investment.Holdings h ON h.Id=s.HoldingId WHERE {where} ORDER BY s.CreatedAt DESC";
        var map = new Dictionary<long,SIP>();
        await db.QueryAsync<SIP,Holding,SIP>(sql,(s,h)=>{
            if(!map.TryGetValue(s.Id,out var item)){ item=s; item.Holding=h?.Id>0?h:null; map[s.Id]=item; }
            return item;
        },args,splitOn:"Id");
        return map.Values.ToList();
    }

    public async Task<SIP?> GetByIdAsync(long id, CancellationToken ct=default) => (await QueryAsync("s.Id=@Id",new{Id=id})).FirstOrDefault();
    public Task<List<SIP>> GetByUserIdAsync(long userId,CancellationToken ct=default)=>QueryAsync("s.UserId=@UserId",new{UserId=userId});
    public Task<List<SIP>> GetActiveSIPsAsync(CancellationToken ct=default)=>QueryAsync("s.IsActive=1");
    public Task<List<SIP>> GetDueSIPsAsync(DateTime asOfDate,CancellationToken ct=default)=>QueryAsync("s.IsActive=1 AND s.NextExecutionDate<=@AsOfDate",new{AsOfDate=asOfDate});

    public async Task<long> CreateAsync(long userId,string name,long? holdingId,decimal amount,string frequency,int dayOfMonth,DateTime startDate,DateTime? endDate,long sourceAccountId,CancellationToken ct=default)
    {
        using var db=_factory.CreateConnection(); var p=new DynamicParameters(new{UserId=userId,SIPName=name,HoldingId=holdingId,Amount=amount,Frequency=frequency,DayOfMonth=dayOfMonth,StartDate=startDate,EndDate=endDate,SourceAccountId=sourceAccountId});
        p.Add("Id",dbType:System.Data.DbType.Int64,direction:System.Data.ParameterDirection.Output);
        await db.ExecuteAsync("Investment.sp_CreateSIP",p,commandType:System.Data.CommandType.StoredProcedure); return p.Get<long>("Id");
    }
    public async Task UpdateAsync(long id,long userId,string name,long? holdingId,decimal amount,string frequency,int dayOfMonth,DateTime startDate,DateTime? endDate,long sourceAccountId,CancellationToken ct=default)
    { using var db=_factory.CreateConnection(); await db.ExecuteAsync("Investment.sp_UpdateSIP",new{Id=id,UserId=userId,SIPName=name,HoldingId=holdingId,Amount=amount,Frequency=frequency,DayOfMonth=dayOfMonth,StartDate=startDate,EndDate=endDate,SourceAccountId=sourceAccountId},commandType:System.Data.CommandType.StoredProcedure); }
    public async Task SetStatusAsync(long id,long userId,bool isActive,CancellationToken ct=default)
    { using var db=_factory.CreateConnection(); await db.ExecuteAsync("Investment.sp_SetSIPStatus",new{Id=id,UserId=userId,IsActive=isActive},commandType:System.Data.CommandType.StoredProcedure); }
    public async Task DeleteAsync(long id,long userId,CancellationToken ct=default)
    { using var db=_factory.CreateConnection(); await db.ExecuteAsync("Investment.sp_DeleteSIP",new{Id=id,UserId=userId},commandType:System.Data.CommandType.StoredProcedure); }

    public async Task<SIP> AddAsync(SIP e,CancellationToken ct=default){e.Id=await CreateAsync(e.UserId,e.Name,e.HoldingId,e.Amount,e.Frequency.ToString(),e.DayOfMonth,e.StartDate,e.EndDate,e.SourceAccountId??0,ct);return e;}
    public Task UpdateAsync(SIP e,CancellationToken ct=default)=>UpdateAsync(e.Id,e.UserId,e.Name,e.HoldingId,e.Amount,e.Frequency.ToString(),e.DayOfMonth,e.StartDate,e.EndDate,e.SourceAccountId??0,ct);
    public Task RemoveAsync(SIP e,CancellationToken ct=default)=>DeleteAsync(e.Id,e.UserId,ct);
    public async Task<PagedResult<SIP>> PagedAsync(PagedQuery q,string schema,string tableName,string whereClause="",object? param=null,CancellationToken ct=default){var all=await QueryAsync("1=1");return new(){Items=all.Skip((q.PageNumber-1)*q.PageSize).Take(q.PageSize).ToList(),TotalCount=all.Count,Page=q.PageNumber,PageSize=q.PageSize};}
    public async Task<long> CountAsync(string schema,string tableName,string whereClause="",object? param=null,CancellationToken ct=default)=>(await QueryAsync("1=1")).Count;
}
