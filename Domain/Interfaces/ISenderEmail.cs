namespace OnboardingWorker.Service;

public interface ISenderEmail
{
    Task SendeEmail(string email, int id);
}