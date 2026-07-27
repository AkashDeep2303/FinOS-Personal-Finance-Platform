using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Investment.Domain.Entities;
using FinOS.Investment.Domain.Interfaces;

namespace FinOS.Investment.Infrastructure.Repositories;

public class EPFAccountRepository : IEPFAccountRepository
{
    private readonly IConnectionFactory _factory;
    public EPFAccountRepository(IConnectionFactory factory)=>_factory=factory;

    public async Task<EPFAccount?> GetByIdAsync(long id,CancellationToken ct=default)=>await GetWithContributionsAsync(id,ct);
    public async Task<List<EPFAccount>> GetByUserIdAsync(long userId,CancellationToken ct=default)
    { using var db=_factory.CreateConnection(); return await Query(db,"e.UserId=@UserId",new{UserId=userId}); }
    public async Task<EPFAccount?> GetWithContributionsAsync(long id,CancellationToken ct=default)
    { using var db=_factory.CreateConnection(); return (await Query(db,"e.Id=@Id",new{Id=id})).FirstOrDefault(); }

    private static async Task<List<EPFAccount>> Query(System.Data.IDbConnection db,string where,object args)
    {
        var map=new Dictionary<long,EPFAccount>();
        await db.QueryAsync<EPFAccount,EPFContribution,EPFAccount>($@"SELECT e.*,c.* FROM Investment.EPFAccounts e LEFT JOIN Investment.EPFContributions c ON c.EPFAccountId=e.Id WHERE {where} ORDER BY c.Month DESC",(a,c)=>{
            if(!map.TryGetValue(a.Id,out var item)){item=a;item.Contributions=new();map[a.Id]=item;}
            if(c?.Id>0)item.Contributions.Add(c); return item;
        },args,splitOn:"Id"); return map.Values.ToList();
    }

    public async Task<long> CreateAccountAsync(long userId,string? uan,string? code,string? employer,decimal employeePct,decimal employerPct,decimal salary,decimal balance,decimal rate,DateTime startDate,CancellationToken ct=default)
    { using var db=_factory.CreateConnection();var p=new DynamicParameters(new{UserId=userId,UAN=uan,EstablishmentCode=code,EmployerName=employer,EmployeeContributionPct=employeePct,EmployerContributionPct=employerPct,MonthlySalary=salary,CurrentBalance=balance,InterestRate=rate,StartDate=startDate});p.Add("Id",dbType:System.Data.DbType.Int64,direction:System.Data.ParameterDirection.Output);await db.ExecuteAsync("Investment.sp_CreateEPFAccount",p,commandType:System.Data.CommandType.StoredProcedure);return p.Get<long>("Id");}
    public async Task<EPFContribution> AddContributionAsync(long accountId,long userId,DateTime month,decimal salary,CancellationToken ct=default)
    { using var db=_factory.CreateConnection();return await db.QuerySingleAsync<EPFContribution>("Investment.sp_AddEPFContribution",new{EPFAccountId=accountId,UserId=userId,Month=month,MonthlySalary=salary},commandType:System.Data.CommandType.StoredProcedure);}

    public async Task<EPFAccount> AddAsync(EPFAccount e,CancellationToken ct=default){e.Id=await CreateAccountAsync(e.UserId,e.UAN,e.EstablishmentCode,e.EmployerName,e.EmployeeContributionPct,e.EmployerContributionPct,e.MonthlySalary,e.CurrentBalance,e.InterestRate,e.StartDate,ct);return e;}
    public Task UpdateAsync(EPFAccount e,CancellationToken ct=default)=>Task.CompletedTask;
    public Task RemoveAsync(EPFAccount e,CancellationToken ct=default)=>Task.CompletedTask;
    public async Task<PagedResult<EPFAccount>> PagedAsync(PagedQuery q,string schema,string tableName,string whereClause="",object? param=null,CancellationToken ct=default){var all=await GetByUserIdAsync(0,ct);return new(){Items=all,TotalCount=all.Count,Page=q.PageNumber,PageSize=q.PageSize};}
    public async Task<long> CountAsync(string schema,string tableName,string whereClause="",object? param=null,CancellationToken ct=default){using var db=_factory.CreateConnection();return await db.ExecuteScalarAsync<long>($"SELECT COUNT(*) FROM [{schema}].[{tableName}]");}
}
