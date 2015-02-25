using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuantKit
{
    class RenameUtil
    {
        public static Dictionary<string, string> EnumRenameDict = new Dictionary<string, string>()
        {
            {"SmartQuant.GEnum0", "Level2UpdateAction"}
        };
        public static Dictionary<string, string> ClassRenameDict = new Dictionary<string, string>()
        {
            {"SmartQuant.GStream0",    "Level2UpdateStreamer"},
            {"SmartQuant.GInterface2", "INewsProvider"},
            {"SmartQuant.GInterface3", "ICommissionProvider"},
            {"SmartQuant.GInterface4", "IExecutionProvider"},
            {"SmartQuant.GInterface5", "IExecutionSimulator"},
            {"SmartQuant.GInterface6", "ICurrencyConverter"},
            {"SmartQuant.GInterface7", "IInstrumentProvider"}
        };

        public static Dictionary<string, Tuple<Dictionary<string, string>, Dictionary<string, string>>> FieldAndMethodRenameDict = new Dictionary<string, Tuple<Dictionary<string, string>, Dictionary<string, string>>>()
        {
#region rename list
{ "SmartQuant.BarFactory", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"itemLists","itemLists"},
		{"sortedList_0","sortedList_0"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		{"method_2","method_2"},
		{"method_3","method_3"},
		})
},
{ "SmartQuant.BarFactoryItem", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"factory","m_factory"},
		{"instrument","m_instrument"},
		{"barType","m_barType"},
		{"barSize","m_barSize"},
		{"barInput","m_barInput"},
		{"sessionEnabled","m_sessionEnabled"},
		{"session1","session1"},
		{"session2","session2"},
		{"bar","bar"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		})
},
{ "SmartQuant.BarSliceFactory", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"idArray_0","idArray_0"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		{"method_2","method_2"},
		})
},
{ "SmartQuant.TimeBarFactoryItem", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"clockType_0","clockType_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.Clock", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"thread_0","m_thread"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		})
},
{ "SmartQuant.Reminder", Tuple.Create( new Dictionary<string, string>(){
		// field 
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		})
},
{ "SmartQuant.AccountDataManager", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"dictionary_0","m_dictionary"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		{"method_2","method_2"},
		{"method_3","method_3"},
		{"method_4","method_4"},
		})
},
{ "SmartQuant.ObjectStreamer", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"typeId","m_typeId"},
		{"type","m_type"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.DataCount", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"InstrumentId","m_instrumentId"},
		{"Bid","m_bid"},
		{"Ask","m_ask"},
		{"Trade","m_trade"},
		{"Level2","m_level2"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.DataFileServerClient_", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"object_0","object_0"},
		{"tcpClient_0","tcpClient_0"},
		{"dataFileManager_0","dataFileManager_0"},
		{"thread_0","m_thread"},
		{"dataFile_0","dataFile_0"},
		{"streamerManager_0","streamerManager_0"},
		{"int_0","int_0"},
		{"idArray_0","idArray_0"},
		{"quickLZ_0","quickLZ_0"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		})
},
{ "SmartQuant.DataProcessor", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"class52_0","class52_0"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		})
},
{ "SmartQuant.BarSeries", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"bar_0","bar_0"},
		{"bar_1","bar_1"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.QuickLZ", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"byte_0","byte_0"},
		{"byte_1","byte_1"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.DataFile", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"string_0","string_0"},
		{"byte_0","byte_0"},
		{"long_0","long_0"},
		{"long_1","long_1"},
		{"long_2","long_2"},
		{"long_3","long_3"},
		{"int_0","int_0"},
		{"int_1","int_1"},
		{"int_2","int_2"},
		{"int_3","int_3"},
		{"string_1","string_1"},
		{"stream_0","stream_0"},
		{"fileMode_0","fileMode_0"},
		{"bool_0","bool_0"},
		{"bool_1","bool_1"},
		{"list_0","list_0"},
		{"objectKey_0","objectKey_0"},
		{"objectKey_1","objectKey_1"},
		{"streamerManager_0","streamerManager_0"},
		{"bool_2","bool_2"},
		{"memoryStream_0","memoryStream_0"},
		{"binaryWriter_0","binaryWriter_0"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		{"method_2","method_2"},
		{"method_3","method_3"},
		{"method_4","method_4"},
		{"method_5","method_5"},
		{"method_6","method_6"},
		{"method_7","method_7"},
		{"method_8","method_8"},
		})
},
{ "SmartQuant.DataFileManager", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"string_0","string_0"},
		{"dictionary_0","m_dictionary"},
		{"streamerManager_0","streamerManager_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.DataFileServer", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"int_0","int_0"},
		{"ipaddress_0","ipaddress_0"},
		{"tcpListener_0","tcpListener_0"},
		{"dataFileManager_0","dataFileManager_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.DataFileServerClient", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"object_0","object_0"},
		{"tcpClient_0","tcpClient_0"},
		{"fileManager_0","fileManager_0"},
		{"thread_0","m_thread"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		})
},
{ "SmartQuant.DataFilter", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"instrument_0","instrument_0"},
		{"bid_0","bid_0"},
		{"ask_0","ask_0"},
		{"trade_0","trade_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.ObjectKey", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"dataFile_0","dataFile_0"},
		{"class45_0","class45_0"},
		{"string_0","string_0"},
		{"bool_0","bool_0"},
		{"dateTime_0","dateTime_0"},
		{"long_0","long_0"},
		{"int_0","int_0"},
		{"int_1","int_1"},
		{"int_2","int_2"},
		{"byte_0","byte_0"},
		{"byte_1","byte_1"},
		{"string_1","string_1"},
		{"changed","changed"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		})
},
{ "SmartQuant.DataManager", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"idArray_0","idArray_0"},
		{"idArray_1","idArray_1"},
		{"idArray_2","idArray_2"},
		{"idArray_3","idArray_3"},
		{"idArray_4","idArray_4"},
		{"idArray_5","idArray_5"},
		{"idArray_6","idArray_6"},
		{"thread_0","m_thread"},
		{"bool_0","bool_0"},
		{"dictionary_0","m_dictionary"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		{"method_2","method_2"},
		{"method_3","method_3"},
		{"method_4","method_4"},
		{"method_5","method_5"},
		{"method_6","method_6"},
		{"method_7","method_7"},
		{"method_8","method_8"},
		{"method_9","method_9"},
		{"method_10","method_10"},
		{"method_11","method_11"},
		{"method_12","method_12"},
		{"method_13","method_13"},
		})
},
{ "SmartQuant.DataSeries", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"dataFile_0","dataFile_0"},
		{"objectKey_0","objectKey_0"},
		{"int_0","int_0"},
		{"long_1","long_1"},
		{"long_2","long_2"},
		{"bool_0","bool_0"},
		{"bool_1","bool_1"},
		{"class44_0","class44_0"},
		{"class44_1","class44_1"},
		{"class44_2","class44_2"},
		{"class44_3","class44_3"},
		{"idArray_0","idArray_0"},
		{"objectKey_1","objectKey_1"},
		{"long_3","long_3"},
		{"bool_3","bool_3"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		{"method_2","method_2"},
		{"method_3","method_3"},
		{"method_4","method_4"},
		{"method_5","method_5"},
		{"method_6","method_6"},
		{"method_7","method_7"},
		{"method_8","method_8"},
		{"method_9","method_9"},
		{"method_10","method_10"},
		{"method_11","method_11"},
		{"method_12","method_12"},
		{"method_13","method_13"},
		{"method_14","method_14"},
		{"method_15","method_15"},
		{"method_16","method_16"},
		{"method_17","method_17"},
		{"method_18","method_18"},
		})
},
{ "SmartQuant.DataSeriesIterator", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"dataSeries_0","dataSeries_0"},
		{"long_0","long_0"},
		{"long_1","long_1"},
		{"long_2","long_2"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.DataServer", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"framework","framework"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.Provider", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"dataQueue","m_dataQueue"},
		{"executionQueue","m_executionQueue"},
		{"historicalQueue","m_historicalQueue"},
		{"instrumentQueue","m_instrumentQueue"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		{"method_2","method_2"},
		{"method_3","method_3"},
		})
},
{ "SmartQuant.DataSimulator", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"thread_0","m_thread"},
		{"linkedList_0","linkedList_0"},
		{"long_0","long_0"},
		{"bool_0","bool_0"},
		{"bool_1","bool_1"},
	},new Dictionary<string,string>(){
		// method 
		{"method_4","method_4"},
		{"method_5","method_5"},
		{"method_6","method_6"},
		})
},
{ "SmartQuant.Field", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"double_0","double_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.FieldList", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"idArray_0","idArray_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.FileDataServer", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"dataFile_0","dataFile_0"},
		{"string_0","string_0"},
		{"idArray_0","idArray_0"},
		{"bool_0","bool_0"},
		{"idArray_1","idArray_1"},
		{"idArray_2","idArray_2"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		})
},
{ "SmartQuant.FileManager", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"string_0","string_0"},
		{"dictionary_0","m_dictionary"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.Fundamental", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"int_0","int_0"},
		{"int_1","int_1"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.IdArray`1", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"int_1","int_1"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		})
},
{ "SmartQuant.Level2Snapshot", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"byte_0","byte_0"},
		{"int_0","int_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.Level2Update", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"byte_0","byte_0"},
		{"int_0","int_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.NetDataFile", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"string_2","string_2"},
		{"int_4","int_4"},
		{"tcpClient_0","tcpClient_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.NetDataFile_", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"string_2","string_2"},
		{"int_4","int_4"},
		{"tcpClient_0","tcpClient_0"},
		{"binaryReader_0","binaryReader_0"},
		{"binaryWriter_1","binaryWriter_1"},
		{"hEkvfxsZvj","hEkvfxsZvj"},
		{"thread_0","m_thread"},
		{"quickLZ_0","quickLZ_0"},
	},new Dictionary<string,string>(){
		// method 
		{"method_9","method_9"},
		{"method_10","method_10"},
		{"method_11","method_11"},
		{"method_12","method_12"},
		{"method_13","method_13"},
		{"method_14","method_14"},
		})
},
{ "SmartQuant.NetDataSeries", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"netDataFile__0","netDataFile__0"},
		{"int_1","int_1"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.News", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"int_0","int_0"},
		{"int_1","int_1"},
		{"byte_0","byte_0"},
		{"string_0","string_0"},
		{"string_1","string_1"},
		{"string_2","string_2"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OrderBook", Tuple.Create( new Dictionary<string, string>(){
		// field 
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		{"method_2","method_2"},
		{"method_3","method_3"},
		})
},
{ "SmartQuant.PermanentQueue`1", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"lhqvQeDuvy","lhqvQeDuvy"},
		{"dictionary_0","m_dictionary"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.EventLoggerManager", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"dictionary_0","m_dictionary"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.EventPipe", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"linkedList_1","linkedList_1"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.EventLogger", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"framework","framework"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.DataSeriesEventLogger", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"dataSeries_0","m_dataSeries"},
		{"dateTime_0","m_dateTime"},
		{"VekuahpnvX","VekuahpnvX"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnHistoricalData", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"historicalData_0","m_historicalData"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnHistoricalDataEnd", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"historicalDataEnd_0","m_historicalDataEnd"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnInstrumentDefinition", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"instrumentDefinition_0","m_instrumentDefinition"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnInstrumentDefinitionEnd", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"instrumentDefinitionEnd_0","m_instrumentDefinitionEnd"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnNewOrder", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"order_0","m_order"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnSendOrder", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"order_0","m_order"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnPendingNewOrder", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"order_0","m_order"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnQueueClosed", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"eventQueue_0","eventQueue_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnQueueOpened", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"eventQueue_0","eventQueue_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnSimulatorProgress", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"long_0","long_0"},
		{"int_0","int_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnStrategyEvent", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"object_0","object_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnSubscribe", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"instrumentList_0","m_instrumentList"},
		{"dateTime_0","dateTime_0"},
		{"dateTime_1","dateTime_1"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnUnsubscribe", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"instrumentList_0","m_instrumentList"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnUserCommand", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"string_0","string_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.SortedEventQueue", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"dateTime_0","m_dateTime"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.FrameworkServer", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"dictionary_0","m_dictionary"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		})
},
{ "SmartQuant.MatchingEngine", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"idArray_0","idArray_0"},
	},new Dictionary<string,string>(){
		// method 
		{"method_4","method_4"},
		{"method_5","method_5"},
		{"method_6","method_6"},
		{"method_7","method_7"},
		{"method_8","method_8"},
		{"method_9","method_9"},
		{"method_10","method_10"},
		{"method_11","method_11"},
		{"method_12","method_12"},
		})
},
{ "SmartQuant.OrderFactory", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"idArray_0","idArray_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.QuoteSeries", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"string_0","string_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.StreamerManager", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"idArray_0","idArray_0"},
		{"dictionary_0","m_dictionary"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.SubscriptionManager", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"dictionary_0","m_dictionary"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		{"method_2","method_2"},
		{"method_3","method_3"},
		})
},
{ "SmartQuant.TickSeries", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"tick_0","tick_0"},
		{"tick_1","tick_1"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.TimeSeries", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"timeSeriesItem_0","timeSeriesItem_0"},
		{"timeSeriesItem_1","timeSeriesItem_1"},
		{"bool_0","bool_0"},
		{"double_0","double_0"},
		{"double_1","double_1"},
		{"double_2","double_2"},
		{"double_3","double_3"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.EventBus", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"ieventQueue_0","ieventQueue_0"},
		{"ieventQueue_1","ieventQueue_1"},
		{"leTeUyTkf5","qindex"},
		{"eventQueue_0","eventQueue_0"},
		{"manualResetEventSlim_0","manualResetEventSlim_0"},
		{"event_0","event_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.EventDispatcher", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"frameworkEventHandler_0","frameworkEventHandler_0"},
		{"instrumentEventHandler_0","instrumentEventHandler_0"},
		{"instrumentEventHandler_1","instrumentEventHandler_1"},
		{"instrumentDefinitionEventHandler_0","instrumentDefinitionEventHandler_0"},
		{"instrumentDefinitionEndEventHandler_0","instrumentDefinitionEndEventHandler_0"},
		{"providerEventHandler_0","providerEventHandler_0"},
		{"providerEventHandler_1","providerEventHandler_1"},
		{"providerEventHandler_2","providerEventHandler_2"},
		{"providerEventHandler_3","providerEventHandler_3"},
		{"providerEventHandler_4","providerEventHandler_4"},
		{"executionCommandEventHandler_0","executionCommandEventHandler_0"},
		{"executionReportEventHandler_0","executionReportEventHandler_0"},
		{"orderManagerClearedEventHandler_0","orderManagerClearedEventHandler_0"},
		{"fillEventHandler_0","fillEventHandler_0"},
		{"transactionEventHandler_0","transactionEventHandler_0"},
		{"barEventHandler_0","barEventHandler_0"},
		{"bidEventHandler_0","bidEventHandler_0"},
		{"askEventHandler_0","askEventHandler_0"},
		{"tradeEventHandler_0","tradeEventHandler_0"},
		{"providerErrorEventHandler_0","providerErrorEventHandler_0"},
		{"historicalDataEventHandler_0","historicalDataEventHandler_0"},
		{"historicalDataEndEventHandler_0","historicalDataEndEventHandler_0"},
		{"portfolioEventHandler_0","portfolioEventHandler_0"},
		{"portfolioEventHandler_1","portfolioEventHandler_1"},
		{"positionEventHandler_0","positionEventHandler_0"},
		{"positionEventHandler_1","positionEventHandler_1"},
		{"positionEventHandler_2","positionEventHandler_2"},
		{"portfolioEventHandler_2","portfolioEventHandler_2"},
		{"groupEventHandler_0","groupEventHandler_0"},
		{"groupEventEventHandler_0","groupEventEventHandler_0"},
		{"groupUpdateEventHandler_0","groupUpdateEventHandler_0"},
		{"simulatorProgressEventHandler_0","simulatorProgressEventHandler_0"},
		{"eventHandler_0","eventHandler_0"},
		{"accountDataEventHandler_0","accountDataEventHandler_0"},
		{"eventHandler_1","eventHandler_1"},
		{"eventHandler_2","eventHandler_2"},
		{"AnwCbqQgIv","AnwCbqQgIv"},
		{"eventHandler_3","eventHandler_3"},
		{"eventHandler_4","eventHandler_4"},
		{"framework","framework"},
		{"eventQueue_0","eventQueue_0"},
		{"thread_0","m_thread"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		})
},
{ "SmartQuant.EventManager", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"eventBus_0","eventBus_0"},
		{"thread_0","m_thread"},
		{"bool_0","bool_0"},
		{"stopwatch_0","stopwatch_0"},
		{"bool_1","bool_1"},
		{"byte_0","m_eventTypeId"},
		{"idArray_0","idArray_0"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		{"method_2","method_2"},
		{"method_3","method_3"},
		{"method_4","method_4"},
		{"method_5","method_5"},
		{"method_6","method_6"},
		{"method_7","method_7"},
		{"method_8","method_8"},
		{"method_9","method_9"},
		{"method_10","method_10"},
		{"method_11","method_11"},
		{"method_12","method_12"},
		{"method_13","method_13"},
		{"method_14","method_14"},
		{"method_15","method_15"},
		{"method_16","method_16"},
		{"method_17","method_17"},
		{"method_18","method_18"},
		{"method_19","method_19"},
		{"method_20","method_20"},
		{"method_21","method_21"},
		{"method_22","method_22"},
		{"method_23","method_23"},
		{"method_24","method_24"},
		{"method_25","method_25"},
		{"method_26","method_26"},
		{"method_27","method_27"},
		{"method_28","method_28"},
		{"method_29","method_29"},
		{"method_30","method_30"},
		{"method_31","method_31"},
		{"method_32","method_32"},
		{"method_33","method_33"},
		{"method_34","method_34"},
		{"method_35","method_35"},
		{"method_36","method_36"},
		{"method_37","method_37"},
		{"method_38","method_38"},
		{"method_39","method_39"},
		{"method_40","method_40"},
        {"IwvCpGfiJb", "IwvCpGfiJb"}
		})
},
{ "SmartQuant.EventQueue", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"eventBus_0","eventBus_0"},
		{"event_0","event_0"},
		{"int_1","int_1"},
		{"int_2","int_2"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.EventServer", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"eventQueue_0","eventQueue_0"},
		{"eventBus_0","eventBus_0"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		{"method_2","method_2"},
		{"method_3","method_3"},
		{"method_4","method_4"},
		{"method_5","method_5"},
		{"method_6","method_6"},
		{"method_7","method_7"},
		{"method_8","method_8"},
		{"method_9","method_9"},
		{"method_10","method_10"},
		{"method_11","method_11"},
		{"method_12","method_12"},
		{"method_13","method_13"},
		{"method_14","method_14"},
		{"method_15","method_15"},
		{"method_16","method_16"},
		{"method_17","method_17"},
		{"method_18","method_18"},
		})
},
{ "SmartQuant.EventSortedSet", Tuple.Create( new Dictionary<string, string>(){
		// field 
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		})
},
{ "SmartQuant.OnExecutionCommand", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"executionCommand_0","m_executionCommand"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnExecutionReport", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"executionReport_0","m_executionReport"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnInstrumentAdded", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"instrument_0","m_instrument"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnInstrumentDeleted", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"instrument_0","m_instrument"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnOrderCancelled", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"order_0","m_order"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnOrderDone", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"order_0","m_order"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnOrderFilled", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"order_0","m_order"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnOrderPartiallyFilled", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"order_0","m_order"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnOrderReplaced", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"order_0","m_order"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnOrderStatusChanged", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"order_0","m_order"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnPositionChanged", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"portfolio","m_portfolio"},
		{"position","m_position"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnPositionClosed", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"portfolio","m_portfolio"},
		{"position","m_position"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnPositionOpened", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"portfolio","m_portfolio"},
		{"position","m_position"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnProviderAdded", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"iprovider_0","m_iprovider"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnProviderRemoved", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"provider_0","m_provider"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnProviderStatusChanged", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"provider_0","m_provider"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.OnSimulatorStart", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"dateTime_0","dateTime_0"},
		{"dateTime_1","dateTime_1"},
		{"long_0","long_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.GroupDispatcher", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"idArray_0","idArray_0"},
		{"list_0","list_0"},
		{"dictionary_0","m_dictionary"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		{"method_2","method_2"},
		{"method_3","method_3"},
		{"method_4","method_4"},
		})
},
{ "SmartQuant.Indicator", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"input","m_input"},
		{"calculate","m_calculate"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		})
},
{ "SmartQuant.Configuration", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"IsInstrumentFileLocal","IsInstrumentFileLocal"},
		{"InstrumentFileHost","InstrumentFileHost"},
		{"InstrumentFilePort","InstrumentFilePort"},
		{"InstrumentFileName","InstrumentFileName"},
		{"IsDataFileLocal","IsDataFileLocal"},
		{"DataFileHost","DataFileHost"},
		{"DataFilePort","DataFilePort"},
		{"DataFileName","DataFileName"},
		{"OrderServer","OrderServer"},
		{"DefaultCurrency","DefaultCurrency"},
		{"DefaultExchange","DefaultExchange"},
		{"ProviderManagerFileName","ProviderManagerFileName"},
		{"Streamers","Streamers"},
		{"Providers","Providers"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.ExecutionSimulator", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"idArray_0","idArray_0"},
	},new Dictionary<string,string>(){
		// method 
		{"method_4","method_4"},
		{"method_5","method_5"},
		{"method_6","method_6"},
		{"method_7","method_7"},
		})
},
{ "SmartQuant.Order", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"double_6","double_6"},
		{"bool_0","bool_0"},
		{"bool_1","bool_1"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		})
},
{ "SmartQuant.OrderManager", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"list_0","list_0"},
		{"idArray_0","idArray_0"},
		{"dictionary_0","m_dictionary"},
		{"int_0","int_0"},
		{"orderServer_0","orderServer_0"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		{"method_2","method_2"},
		{"method_3","method_3"},
		})
},
{ "SmartQuant.Stop", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"initPrice","m_initPrice"},
		{"currPrice","m_currPrice"},
		{"stopPrice","m_stopPrice"},
		{"fillPrice","m_fillPrice"},
		{"trailPrice","m_trailPrice"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		{"method_2","method_2"},
		{"method_3","method_3"},
		{"method_4","method_4"},
		{"method_5","method_5"},
		{"method_6","method_6"},
		{"method_7","method_7"},
		{"method_8","method_8"},
		{"method_9","method_9"},
		})
},
{ "SmartQuant.Framework", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"bool_1","bool_1"},
		{"bool_2","bool_2"},
		{"bool_3","bool_3"},
		{"bool_4","bool_4"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		{"method_2","method_2"},
		{"method_3","method_3"},
		{"method_4","method_4"},
		{"method_5","method_5"},
		})
},
{ "SmartQuant.AltIdList", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"idArray_0","idArray_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.InstrumentServer", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"framework","m_framework"},
		{"instruments","m_instruments"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.FileInstrumentServer", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"dataFile_0","dataFile_0"},
		{"bool_0","bool_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.Instrument", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"bool_0","bool_0"},
		{"bool_1","bool_1"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		})
},
{ "SmartQuant.InstrumentList", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"dictionary_0","m_dictionary"},
		{"idArray_0","idArray_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.InstrumentManager", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"instrumentList_1","instrumentList_1"},
		{"int_0","int_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.Leg", Tuple.Create( new Dictionary<string, string>(){
		// field 
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		})
},
{ "SmartQuant.Group", Tuple.Create( new Dictionary<string, string>(){
		// field 
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		})
},
{ "SmartQuant.GroupManager", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"int_0","int_0"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		})
},
{ "SmartQuant.Strategy", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"idArray_0","idArray_0"},
		{"idArray_1","idArray_1"},
		{"idArray_2","idArray_2"},
		{"raiseEvents","raiseEvents"},
		{"bool_1","bool_1"},
		{"tickSeries_2","tickSeries_2"},
		{"list_0","list_0"},
		{"idArray_3","idArray_3"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		{"method_2","method_2"},
		{"method_3","method_3"},
		{"method_4","method_4"},
		{"method_5","method_5"},
		{"method_6","method_6"},
        		{"vmethod_0","vmethod_0"},
		{"vmethod_1","vmethod_1"},
		{"vmethod_2","vmethod_2"},
		{"vmethod_3","vmethod_3"},
		{"vmethod_4","vmethod_4"},
		{"vmethod_5","vmethod_5"},
		{"vmethod_6","vmethod_6"},
		{"vmethod_7","vmethod_7"},
		{"vmethod_8","vmethod_8"},
		{"vmethod_9","vmethod_9"},
		{"vmethod_10","vmethod_10"},
		{"vmethod_11","vmethod_11"},
		{"vmethod_12","vmethod_12"},
		{"vmethod_13","vmethod_13"},
		{"vmethod_14","vmethod_14"},
		{"vmethod_15","vmethod_15"},
		{"vmethod_16","vmethod_16"},
		{"vmethod_17","vmethod_17"},
		{"vmethod_18","vmethod_18"},
		{"vmethod_19","vmethod_19"},
		{"vmethod_20","vmethod_20"},
		{"vmethod_21","vmethod_21"},
		{"vmethod_22","vmethod_22"},
		{"vmethod_23","vmethod_23"},
		{"vmethod_24","vmethod_24"},
		{"vmethod_25","vmethod_25"},
		{"vmethod_26","vmethod_26"},
		{"vmethod_27","vmethod_27"},
		{"vmethod_28","vmethod_28"},
		{"vmethod_29","vmethod_29"},
		{"vmethod_30","vmethod_30"},
		{"vmethod_31","vmethod_31"},
		{"vmethod_32","vmethod_32"},
		{"vmethod_33","vmethod_33"},
		{"vmethod_34","vmethod_34"},
		{"vmethod_35","vmethod_35"},
		{"vmethod_36","vmethod_36"},
		})
},
{ "SmartQuant.InstrumentStrategy", Tuple.Create( new Dictionary<string, string>(){
		// field 
	},new Dictionary<string,string>(){
		// method 
		{"method_7","method_7"},
        {"vmethod_0","vmethod_0"},
		})
},
{ "SmartQuant.PerformanceStrategy", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"stopwatch_0","stopwatch_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.Scenario", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"strategy","strategy"},
		{"Name","Name"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		})
},
{ "SmartQuant.Account", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"idArray_0","idArray_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.Fill", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"int_0","int_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.FillSeries", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"string_0","string_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.PortfolioStatisticsItem", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"statistics","statistics"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.PortfolioStatisticsItemList", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"idArray_0","idArray_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.Portfolio", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"idArray_0","idArray_0"},
		{"idArray_1","idArray_1"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		{"method_2","method_2"},
		{"method_3","method_3"},
		{"method_4","method_4"},
		})
},
{ "SmartQuant.PortfolioList", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"idArray_0","idArray_0"},
		{"dictionary_0","m_dictionary"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		})
},
{ "SmartQuant.PortfolioManager", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"int_0","int_0"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		{"method_2","method_2"},
		{"method_3","method_3"},
		})
},
{ "SmartQuant.PortfolioPerformance", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"eventHandler_0","eventHandler_0"},
		{"portfolio_0","portfolio_0"},
		{"dateTime_0","dateTime_0"},
		{"double_0","double_0"},
		{"double_1","double_1"},
		{"double_2","double_2"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.PortfolioStatistics", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"portfolio_0","portfolio_0"},
		{"idArray_0","idArray_0"},
		{"idArray_1","idArray_1"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		{"method_2","method_2"},
		{"method_3","method_3"},
		{"method_4","method_4"},
		{"method_5","method_5"},
		{"method_6","method_6"},
		{"method_7","method_7"},
		{"method_8","method_8"},
		{"method_9","method_9"},
		{"method_10","method_10"},
		})
},
{ "SmartQuant.TradeDetector", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"instrument_0","instrument_0"},
		{"double_0","double_0"},
		{"timeSeries_0","timeSeries_0"},
		{"portfolio_0","portfolio_0"},
		{"delegate7_0","delegate7_0"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		{"method_2","method_2"},
		{"method_3","method_3"},
		})
},
{ "SmartQuant.ProviderPropertyList", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"dictionary_0","m_dictionary"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		})
},
{ "SmartQuant.XmlProvider", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"ProviderId","ProviderId"},
		{"InstanceId","InstanceId"},
		{"Properties","Properties"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.ProviderId", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"class73_0","class73_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.ProviderList", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"idArray_0","idArray_0"},
		{"dictionary_0","m_dictionary"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.ProviderManager", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"cCsbsyqlso","cCsbsyqlso"},
		{"providerList_1","providerList_1"},
		{"providerList_2","providerList_2"},
		{"providerList_3","providerList_3"},
		{"providerList_4","providerList_4"},
		{"class69_0","class69_0"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		{"method_2","method_2"},
		{"method_3","method_3"},
		})
},
{ "SmartQuant.XmlProviderManagerSettings", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"Providers","Providers"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.XmlProviderProperty", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"Name","Name"},
		{"Value","Value"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.ReportItem", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"id","id"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.Report", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"list_0","list_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.TradeCountReportItem", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"int_0","int_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.ServerConfiguration", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"TypeName","TypeName"},
		{"ConnectionString","ConnectionString"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.AttributeStreamer", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"string_0","string_0"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		{"method_2","method_2"},
		})
},
{ "SmartQuant.MetaStrategy", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"idArray_4","idArray_4"},
		{"idArray_5","idArray_5"},
		{"idArray_6","idArray_6"},
		{"list_1","list_1"},
	},new Dictionary<string,string>(){
		// method 
        {"vmethod_15","vmethod_15"},
		{"vmethod_11","vmethod_11"},
		{"vmethod_9","vmethod_9"},
		{"vmethod_10","vmethod_10"},
		{"vmethod_19","vmethod_19"},
		{"vmethod_26","vmethod_26"},
		{"vmethod_28","vmethod_28"},
		{"vmethod_24","vmethod_24"},
		{"vmethod_25","vmethod_25"},
		{"vmethod_27","vmethod_27"},
		{"vmethod_23","vmethod_23"},
		{"vmethod_33","vmethod_33"},
		{"vmethod_31","vmethod_31"},
		{"vmethod_32","vmethod_32"},
		{"vmethod_29","vmethod_29"},
		{"vmethod_6","vmethod_6"},
		{"vmethod_7","vmethod_7"},
		{"vmethod_0","vmethod_0"},
		{"vmethod_1","vmethod_1"},
		})
},
{ "SmartQuant.ParameterList", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"methods","methods"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.SellSideInstrumentStrategy", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"idArray_4","idArray_4"},
		{"idArray_5","idArray_5"},
		{"list_1","list_1"},
		{"Instrument","Instrument"},
	},new Dictionary<string,string>(){
		// method 
		{"method_7","method_7"},
		})
},
{ "SmartQuant.StrategyList", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"idArray_0","idArray_0"},
	},new Dictionary<string,string>(){
		// method 
		})
},
{ "SmartQuant.StrategyManager", Tuple.Create( new Dictionary<string, string>(){
		// field 
		{"strategyStatus_0","strategyStatus_0"},
		{"dictionary_0","m_dictionary"},
		{"byte_0","byte_0"},
		{"strategy_0","strategy_0"},
	},new Dictionary<string,string>(){
		// method 
		{"method_0","method_0"},
		{"method_1","method_1"},
		{"method_2","method_2"},
		{"method_3","method_3"},
		{"method_4","method_4"},
		{"method_5","method_5"},
		{"method_6","method_6"},
		{"method_7","method_7"},
		{"method_8","method_8"},
		{"method_9","method_9"},
		{"method_10","method_10"},
		{"method_11","method_11"},
		{"method_12","method_12"},
		{"method_13","method_13"},
		{"method_14","method_14"},
		{"method_15","method_15"},
		{"method_16","method_16"},
		{"method_17","method_17"},
		{"method_18","method_18"},
		{"method_19","method_19"},
		{"method_20","method_20"},
		{"method_21","method_21"},
		{"method_22","method_22"},
		{"method_23","method_23"},
		{"method_24","method_24"},
		{"method_25","method_25"},
		{"method_26","method_26"},
		{"method_27","method_27"},
		{"method_28","method_28"},
		{"method_29","method_29"},
		{"method_30","method_30"},
		{"method_31","method_31"},
		{"method_32","method_32"},
		{"method_33","method_33"},
		{"method_34","method_34"},
		{"method_35","method_35"},
		{"method_36","method_36"},
		})
}
#endregion
};
    }
}
