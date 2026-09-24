namespace AccountManagement
{
    public abstract class Account
    {
        public int accountId { get; set; }
        public required string username { get; set; }
        protected string _password = string.Empty;
        public override int GetHashCode() => (username, _password).GetHashCode();

        public bool verifyPassword(string password)
        {
            return PasswordHasher.VerifyPassword(password, _password);
        }
        public string setPassword(string newPassword)
        {
            _password = PasswordHasher.HashPassword(newPassword);
            return _password;
        }
    }
}