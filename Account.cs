namespace AccountManagement
{
    public abstract class Account
    {
        public required string username { get; set; }
        protected string _password = string.Empty;
        public override int GetHashCode() => (username, _password).GetHashCode();

        public string getPassword()
        {
            string Password = _password;
            return Password;
        }
        public string setPassword(string newPassword)
        {
            _password = newPassword;
            return _password;
        }
    }
}