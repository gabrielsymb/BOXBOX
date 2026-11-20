using Boxbox.Verification;

foreach (var check in VerificationCatalog.Checks)
{
    check.Run();
    Console.WriteLine($"PASS: {check.Name}");
}
