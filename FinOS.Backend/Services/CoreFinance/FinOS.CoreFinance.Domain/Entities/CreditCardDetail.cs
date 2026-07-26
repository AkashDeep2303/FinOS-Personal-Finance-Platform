namespace FinOS.CoreFinance.Domain.Entities;
public class CreditCardDetail
{
 public long AccountId{get;set;} public long UserId{get;set;} public string Name{get;set;}=""; public string? InstitutionName{get;set;}
 public decimal Balance{get;set;} public decimal CreditLimit{get;set;} public byte? StatementDay{get;set;} public byte? PaymentDueDay{get;set;}
 public decimal MinimumAmountDue{get;set;} public decimal TotalAmountDue{get;set;} public decimal AnnualInterestRate{get;set;}
 public DateTime? LastPaymentDate{get;set;} public decimal? LastPaymentAmount{get;set;}
 public decimal Outstanding=>Math.Abs(Math.Min(0,Balance)); public decimal AvailableCredit=>Math.Max(0,CreditLimit-Outstanding);
 public decimal UtilizationPct=>CreditLimit<=0?0:Math.Round(Outstanding/CreditLimit*100,2);
}
