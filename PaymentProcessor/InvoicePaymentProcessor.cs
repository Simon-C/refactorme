using System;
using System.Linq;
using PaymentProcessor.Extensions;

namespace PaymentProcessor
{
	public class InvoicePaymentProcessor
	{
		private readonly InvoiceRepository _invoiceRepository;

		public InvoicePaymentProcessor( InvoiceRepository invoiceRepository )
		{
			_invoiceRepository = invoiceRepository;
		}

		private string ValidatePayment( Payment payment, Invoice invoice )
		{
			if ( invoice == null )
				throw new InvalidOperationException("There is no invoice matching this payment");

			if ( invoice.Amount == 0 )
			{
				if ( invoice.Payments.IsNullOrEmpty() )
					return "no payment needed";

				throw new InvalidOperationException("The invoice is in an invalid state, it has an amount of 0 and it has payments.");
			}

			var totalPayments = invoice.Payments.Sum( x => x.Amount );
			var outstandingBalance = invoice.Amount - invoice.AmountPaid;

			if ( payment.Amount > invoice.Amount )
				return "the payment is greater than the invoice amount";

			if ( invoice.Amount == totalPayments )
				return "invoice was already fully paid";

			if ( payment.Amount > outstandingBalance )
				return "the payment is greater than the partial amount remaining";

			return null;
		}

		public string ProcessPayment( Payment payment )
		{
			var invoice = _invoiceRepository.GetInvoice( payment.Reference );

			var validatePayment = ValidatePayment( payment, invoice );

			if ( !validatePayment.IsNullOrEmpty() )
				return validatePayment;

			if ( !invoice.Payments.IsNullOrEmpty() )
			{
				invoice.AddPayment( payment );

				var outstandingBalance = invoice.Amount - invoice.AmountPaid;

				if ( outstandingBalance == 0 )
				{
					return "final partial payment received, invoice is now fully paid";
				}
				else
				{
					return "another partial payment received, still not fully paid";
				}
			}
			else
			{
				invoice.AddPayment( payment );

				if ( invoice.Amount == payment.Amount )
				{
					return "invoice is now fully paid";
				}
				else
				{
					return "invoice is now partially paid";
				}
			}
		}
	}
}