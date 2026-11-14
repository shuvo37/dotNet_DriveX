namespace DriveX.Enums;

// Stored as text in the payment_status column, so adding values needs no migration.
//  PENDING - payment row created, customer is on the gateway page, money not confirmed yet
//  DONE    - the gateway confirmed the money arrived
//  FAILED  - the gateway reported a failed or cancelled payment
public enum PaymentStatus
{
    PENDING,
    DONE,
    FAILED
}
