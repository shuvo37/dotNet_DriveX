namespace DriveX.DTOs.Cars;

// Returned after add, update and active-status change: { "message": "...", "car": { ... } }
public record CarResultResponse(string Message, CarResponse Car);
