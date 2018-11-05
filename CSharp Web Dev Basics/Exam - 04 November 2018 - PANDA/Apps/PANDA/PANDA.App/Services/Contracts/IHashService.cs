namespace PANDA.App.Services.Contracts
{
    public interface IHashService
    {
        byte[] GenerateSalt();
        string HashPassword(string password, byte[] salt);
        bool IsPasswordValid(string enteredPassword, string base64Hash);
    }
}