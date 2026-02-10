namespace CodeArena.Application.Exceptions;

public class AlreadyAcceptedException(string message = "Problem already solved.") : Exception(message);
