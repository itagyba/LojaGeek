﻿using LojaGeek.Model.DB.Model;
using LojaGeek.Model.DB.Repository;
using MySql.Data.MySqlClient;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Context;
using NHibernate.Mapping.ByCode;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace LojaGeek.Model.DB
{
    public class DbFactory
    {
        private static DbFactory _instance = null;

        private ISessionFactory _sessionFactory;

        public ClienteRepository ClienteRepository { get; set; }
        public EnderecoRepository EnderecoRepository { get; set; }
        public ProdutoRepository ProdutoRepository { get; set; }
        public InteresseRepository InteresseRepository { get; set; }
        public ComentarioRepository ComentarioRepository { get; set; }

        private DbFactory()
        {
            Conexao();

            ClienteRepository = new ClienteRepository(Session);
            EnderecoRepository = new EnderecoRepository(Session);
            ProdutoRepository = new ProdutoRepository(Session);
            InteresseRepository = new InteresseRepository(Session);
            ComentarioRepository = new ComentarioRepository(Session);
        }

        public static DbFactory Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DbFactory();
                }

                return _instance;
            }
        }

        private void Conexao()
        {
            try
            {
                var server = "localhost";
                var port = "3306";
                var dbName = "lojageek";
                var user = "root";
                var psw = "root";

                var stringConexao = "Persist Security Info=False;" +
                    "server=" + server +
                    ";port=" + port +
                    ";database=" + dbName +
                    ";uid=" + user +
                    ";pwd=" + psw;

                try
                {
                    var mysql = new MySqlConnection(stringConexao);
                    mysql.Open();

                    if (mysql.State == ConnectionState.Open)
                    {
                        mysql.Close();
                    }
                }
                catch
                {
                    CriarSchema(server, port, dbName, psw, user);
                }

                ConfigurarNHibernate(stringConexao);
            }
            catch (Exception ex)
            {
                throw new Exception("Não foi possível conectar", ex);
            }
        }

        private void CriarSchema(string server, string port, string dbName, string psw, string user)
        {
            try
            {
                var stringConexao = "server=" + server +
                    ";user=" + user +
                    ";port=" + port +
                    ";password=" + psw;

                var mySql = new MySqlConnection(stringConexao);

                var cmd = mySql.CreateCommand();

                mySql.Open();
                cmd.CommandText = "CREATE DATABASE IF NOT EXISTS `" + dbName + "`;";
                cmd.ExecuteNonQuery();
                mySql.Close();
            }
            catch (Exception ex)
            {
                throw new Exception("Não foi possível criar o Schema", ex);
            }
        }

        private void ConfigurarNHibernate(String stringConexao)
        {
            try
            {
                var config = new Configuration();

                //Configuração do NHibernate com o MySql
                config.DataBaseIntegration(i =>
                {
                    //Dialeto do banco
                    i.Dialect<NHibernate.Dialect.MySQLDialect>();
                    //Conexão string
                    i.ConnectionString = stringConexao;
                    //Driver de conexão com o banco
                    i.Driver<NHibernate.Driver.MySqlDataDriver>();
                    //Provedor de conexão do MySql
                    i.ConnectionProvider<NHibernate.Connection.DriverConnectionProvider>();
                    //Gera log dos sql executados no console
                    i.LogSqlInConsole = true;
                    //Descomentar caso queira visualizar o log de sql formatado no console
                    i.LogFormattedSql = true;
                    //Cria o schema do banco de dados sempre que a configuration for utilizada
                    i.SchemaAction = SchemaAutoAction.Update;
                });

                //Realiza o mapeamento das classes
                var maps = this.Mapeamento();
                config.AddMapping(maps);

                //Verifico se a aplicação é Desktop ou Web
                if (HttpContext.Current == null)
                {
                    config.CurrentSessionContext<ThreadStaticSessionContext>();
                }
                else
                {
                    config.CurrentSessionContext<WebSessionContext>();
                }

                this._sessionFactory = config.BuildSessionFactory();

            }
            catch (Exception ex)
            {
                throw new Exception("Não foi possível configurar NHibernate", ex);
            }
        }

        private HbmMapping Mapeamento()
        {
            try
            {
                var mapper = new ModelMapper();

                mapper.AddMappings(Assembly.GetAssembly(typeof(ClienteMap)).GetTypes());
                mapper.AddMappings(Assembly.GetAssembly(typeof(EnderecoMap)).GetTypes());
                mapper.AddMappings(Assembly.GetAssembly(typeof(ProdutoMap)).GetTypes());

                return mapper.CompileMappingForAllExplicitlyAddedEntities();
            }
            catch (Exception ex)
            {
                throw new Exception("Não mapeou", ex);
            }
        }

        public ISession Session
        {
            get
            {
                try
                {
                    if (CurrentSessionContext.HasBind(_sessionFactory))
                        return _sessionFactory.GetCurrentSession();

                    var session = _sessionFactory.OpenSession();
                    session.FlushMode = FlushMode.Commit;

                    CurrentSessionContext.Bind(session);

                    return session;
                }
                catch (Exception ex)
                {
                    throw new Exception("Não foi possível criar sessão", ex);
                }
            }
        }
    }
}
