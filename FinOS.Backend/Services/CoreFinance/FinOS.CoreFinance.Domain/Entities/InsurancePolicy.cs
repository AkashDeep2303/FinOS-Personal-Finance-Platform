namespace FinOS.CoreFinance.Domain.Entities;
public class InsurancePolicy
{
 public long Id{get;set;} public long UserId{get;set;} public string PolicyType{get;set;}="";
 public string Provider{get;set;}=""; public string? PolicyNumber{get;set;} public decimal CoverageAmount{get;set;}
 public decimal PremiumAmount{get;set;} public string PremiumFrequency{get;set;}="Annual";
 public DateTime? StartDate{get;set;} public DateTime? EndDate{get;set;} public DateTime? RenewalDate{get;set;}
 public string? Nominee{get;set;} public string? Notes{get;set;} public string Status{get;set;}="Active";
}
