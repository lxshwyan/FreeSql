﻿using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Common;

namespace FreeSql {
	public class FreeSqlBuilder {
		IDistributedCache _cache;
		ILogger _logger;
		DataType _dataType;
		string _masterConnectionString;
		string[] _slaveConnectionString;
		bool _isAutoSyncStructure = false;
		bool _isSyncStructureToLower = false;
		bool _isSyncStructureToUpper = false;
		bool _isConfigEntityFromDbFirst = false;
		bool _isNoneCommandParameter = false;
		bool _isLazyLoading = false;
		Action<DbCommand> _aopCommandExecuting = null;
		Action<DbCommand, string> _aopCommandExecuted = null;

		/// <summary>
		/// 使用缓存，不指定默认使用内存
		/// </summary>
		/// <param name="cache">缓存实现</param>
		/// <returns></returns>
		public FreeSqlBuilder UseCache(IDistributedCache cache) {
			_cache = cache;
			return this;
		}

		/// <summary>
		/// 使用日志，不指定默认输出控制台
		/// </summary>
		/// <param name="logger"></param>
		/// <returns></returns>
		public FreeSqlBuilder UseLogger(ILogger logger) {
			_logger = logger;
			return this;
		}
		/// <summary>
		/// 使用连接串
		/// </summary>
		/// <param name="dataType">数据库类型</param>
		/// <param name="connectionString">数据库连接串</param>
		/// <returns></returns>
		public FreeSqlBuilder UseConnectionString(DataType dataType, string connectionString) {
			_dataType = dataType;
			_masterConnectionString = connectionString;
			return this;
		}
		/// <summary>
		/// 使用从数据库，支持多个
		/// </summary>
		/// <param name="slaveConnectionString">从数据库连接串</param>
		/// <returns></returns>
		public FreeSqlBuilder UseSlave(params string[] slaveConnectionString) {
			_slaveConnectionString = slaveConnectionString;
			return this;
		}
		/// <summary>
		/// 【开发环境必备】自动同步实体结构到数据库，程序运行中检查实体表是否存在，然后创建或修改
		/// </summary>
		/// <param name="value">true:运行时检查自动同步结构, false:不同步结构</param>
		/// <returns></returns>
		public FreeSqlBuilder UseAutoSyncStructure(bool value) {
			_isAutoSyncStructure = value;
			return this;
		}
		/// <summary>
		/// 转小写同步结构
		/// </summary>
		/// <param name="value">true:转小写, false:不转</param>
		/// <returns></returns>
		public FreeSqlBuilder UseSyncStructureToLower(bool value) {
			_isSyncStructureToLower = value;
			return this;
		}
		/// <summary>
		/// 转大写同步结构
		/// </summary>
		/// <param name="value">true:转大写, false:不转</param>
		/// <returns></returns>
		public FreeSqlBuilder UseSyncStructureToUpper(bool value) {
			_isSyncStructureToUpper = value;
			return this;
		}
		/// <summary>
		/// 使用数据库的主键和自增，适用 DbFirst 模式，无须在实体类型上设置 [Column(IsPrimary)] 或者 ConfigEntity。此功能目前可用于 mysql/sqlserver/postgresql。
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public FreeSqlBuilder UseConfigEntityFromDbFirst(bool value) {
			_isConfigEntityFromDbFirst = value;
			return this;
		}
		/// <summary>
		/// 不使用命令参数化执行，针对 Insert/Update，也可临时使用 IInsert/IUpdate.NoneParameter() 
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public FreeSqlBuilder UseNoneCommandParameter(bool value) {
			_isNoneCommandParameter = value;
			return this;
		}
		/// <summary>
		/// 延时加载导航属性对象，导航属性需要声明 virtual
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public FreeSqlBuilder UseLazyLoading(bool value) {
			_isLazyLoading = value;
			return this;
		}
		/// <summary>
		/// 监视数据库命令对象
		/// </summary>
		/// <param name="executing">执行前</param>
		/// <param name="executed">执行后，可监视执行性能</param>
		/// <returns></returns>
		public FreeSqlBuilder UseMonitorCommand(Action<DbCommand> executing, Action<DbCommand, string> executed = null) {
			_aopCommandExecuting = executing;
			_aopCommandExecuted = executed;
			return this;
		}

		public IFreeSql Build() => Build<IFreeSql>();
		public IFreeSql<TMark> Build<TMark>() {
			IFreeSql<TMark> ret = null;
			switch(_dataType) {
				case DataType.MySql: ret = new MySql.MySqlProvider<TMark>(_cache, _logger, _masterConnectionString, _slaveConnectionString); break;
				case DataType.SqlServer: ret = new SqlServer.SqlServerProvider<TMark>(_cache, _logger, _masterConnectionString, _slaveConnectionString); break;
				case DataType.PostgreSQL: ret = new PostgreSQL.PostgreSQLProvider<TMark>(_cache, _logger, _masterConnectionString, _slaveConnectionString); break;
				case DataType.Oracle: ret = new Oracle.OracleProvider<TMark>(_cache, _logger, _masterConnectionString, _slaveConnectionString); break;
				case DataType.Sqlite: ret = new Sqlite.SqliteProvider<TMark>(_cache, _logger, _masterConnectionString, _slaveConnectionString); break;
				default: throw new Exception("未指定 UseConnectionString");
			}
			if (ret != null) {
				ret.CodeFirst.IsAutoSyncStructure = _isAutoSyncStructure;
				
				ret.CodeFirst.IsSyncStructureToLower = _isSyncStructureToLower;
				ret.CodeFirst.IsSyncStructureToUpper = _isSyncStructureToUpper;
				ret.CodeFirst.IsConfigEntityFromDbFirst = _isConfigEntityFromDbFirst;
				ret.CodeFirst.IsNoneCommandParameter = _isNoneCommandParameter;
				ret.CodeFirst.IsLazyLoading = _isLazyLoading;
				var ado = ret.Ado as Internal.CommonProvider.AdoProvider;
				ado.AopCommandExecuting += _aopCommandExecuting;
				ado.AopCommandExecuted += _aopCommandExecuted;
			}
			return ret;
		}
	}
}
