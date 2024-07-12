using Oracle.ManagedDataAccess.Client;

namespace final_backend.Packages
{
    public class Pkg_base
    {
        private string _connectionString;

        public Pkg_base(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected OracleConnection GetConnection()
        {
            return new OracleConnection(_connectionString);
        }
    }
}
