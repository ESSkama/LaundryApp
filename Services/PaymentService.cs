using Laundry.Data;
using Laundry.Models;
using Laundry.Patterns.Singleton;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Laundry.Services
{
    public class PaymentService
    {
        private readonly ApplicationDbContext _context;

        public PaymentService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Generate EFT payment details
        public PaymentInstruction GenerateEFTInstruction(int orderId, string orderNumber, decimal amount)
        {
            var instructions = new List<string>
            {
                "Use your Order Number as payment reference",
                "Payment must reflect within 24 hours",
                "Orders are processed only after payment confirmation",
                "Send proof of payment to payments@sudsandspin.co.za"
            };

            var instruction = new PaymentInstruction
            {
                InstructionId = Guid.NewGuid().ToString(),
                OrderId = orderId,
                OrderNumber = orderNumber,
                Amount = amount,
                PaymentMethod = "EFT",
                BankName = "First National Bank (FNB)",
                AccountName = "SUDS & SPIN Laundry Services",
                AccountNumber = "628 345 67890",
                BranchCode = "250655",
                Reference = $"LAUNDRY-{orderNumber}",
                ExpiryDate = DateTime.Now.AddDays(2),
                InstructionsList = instructions,
                InstructionsJson = JsonConvert.SerializeObject(instructions),
                CreatedAt = DateTime.Now
            };

            _context.PaymentInstructions.Add(instruction);
            _context.SaveChanges();

            OrderLogger.Instance.LogEvent($"EFT instruction generated for order {orderNumber}");

            return instruction;
        }

        // Generate PayFast payment URL
        public PayFastPayment GeneratePayFastPayment(int orderId, string orderNumber, decimal amount, string userEmail)
        {
            var payment = new PayFastPayment
            {
                PaymentId = Guid.NewGuid().ToString(),
                OrderId = orderId,
                OrderNumber = orderNumber,
                Amount = amount,
                MerchantId = "10000100",
                MerchantKey = "46f0cd694581a",
                ReturnUrl = $"/Order/PaymentComplete?orderId={orderId}",
                CancelUrl = $"/Order/PaymentCancelled?orderId={orderId}",
                NotifyUrl = $"/Order/PaymentNotify",
                Email = userEmail,
                NameFirst = userEmail?.Split('@')[0] ?? "Customer",
                PaymentStatus = "Pending",
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddHours(1)
            };

            _context.PayFastPayments.Add(payment);
            _context.SaveChanges();

            payment.PayFastUrl = $"https://sandbox.payfast.co.za/eng/process?merchant_id={payment.MerchantId}&merchant_key={payment.MerchantKey}&amount={payment.Amount}&item_name=Laundry+Order+{orderNumber}&item_description=Laundry+Service&return_url={payment.ReturnUrl}&cancel_url={payment.CancelUrl}&notify_url={payment.NotifyUrl}&email_address={payment.Email}";

            OrderLogger.Instance.LogEvent($"PayFast payment generated for order {orderNumber}");

            return payment;
        }

        public async Task<bool> ConfirmEFTPayment(int orderId, string reference)
        {
            var instruction = await _context.PaymentInstructions
                .FirstOrDefaultAsync(p => p.OrderId == orderId);

            if (instruction == null) return false;

            instruction.IsConfirmed = true;
            instruction.ConfirmedAt = DateTime.Now;
            instruction.ReferenceNumber = reference;
            await _context.SaveChangesAsync();

            OrderLogger.Instance.LogEvent($"EFT payment confirmed manually for order {instruction.OrderNumber}");

            return true;
        }

        // Get EFT instruction - MOVED INSIDE THE CLASS
        public async Task<PaymentInstruction?> GetEFTInstruction(int orderId)
        {
            return await _context.PaymentInstructions
                .FirstOrDefaultAsync(p => p.OrderId == orderId);
        }
    }

    public class PaymentInstruction
    {
        public int Id { get; set; }
        public string InstructionId { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ExpiryDate { get; set; }
        public bool IsConfirmed { get; set; } = false;
        public DateTime? ConfirmedAt { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? InstructionsJson { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public List<string> InstructionsList
        {
            get => string.IsNullOrEmpty(InstructionsJson) ? new List<string>() : JsonConvert.DeserializeObject<List<string>>(InstructionsJson) ?? new List<string>();
            set => InstructionsJson = JsonConvert.SerializeObject(value);
        }
    }

    public class PayFastPayment
    {
        public int Id { get; set; }
        public string PaymentId { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string MerchantId { get; set; } = string.Empty;
        public string MerchantKey { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
        public string NotifyUrl { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NameFirst { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = "Pending";
        public string PayFastUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ExpiresAt { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}