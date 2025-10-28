Option Strict On
Option Explicit On

Imports System
Imports System.Net.Http
Imports System.Text
Imports System.Threading.Tasks
Imports System.Collections.Generic
Imports System.Data
Imports System.Linq
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Public Class CompraProductos

    Public Enum PayType
        CreditCard = 1  ' → PIN_FLD_PAY_TYPE = 1
        Boleto = 2      ' → 2
        DAC = 3         ' → 3
    End Enum

    ' ===== Config =====
    Public Property BASE_URL As String = "http://brmdev.hml.ocpcorp.oi.intranet"
    Private Const PATH_PURCHASE As String = "/BRMCustCustomServices/resources/BRMPurchaseCustomServicesREST/PurchasePlans"
    Private Const PROTOCOL_PREFIX As String = "ORACLE_SAP_TEST_"
    Private Const CONTRACT_PREFIX As String = "TP_"
    Private Const VALIDATION_MAX_ATTEMPTS As Integer = 5
    Private Shared ReadOnly VALIDATION_DELAY As TimeSpan = TimeSpan.FromSeconds(2)

    ' ===== Dependencias =====
    Private Shared ReadOnly _http As HttpClient = New HttpClient() With {.Timeout = TimeSpan.FromSeconds(30)}
    Private ReadOnly _db As BrmOracleQuery = New BrmOracleQuery()

    Private Shared ReadOnly _creditCardTemplate As JObject = JObject.Parse(
"{" &
"\n    \"PIN_FLD_POID\":\"0.0.0.1 /account 505566556 0\"," &
"\n    \"PIN_FLD_BILLINFO_OBJ\":\"0.0.0.1 /billinfo -1 0\"," &
"\n    \"PIN_FLD_PAY_TYPE\":1," &
"\n    \"AC_FLD_PROTOCOL_ID\": \"ORACLE_SAP_004\"," &
"\n    \"AC_FLD_NSU_CIELO\": \"123456\"," &
"\n    \"AC_FLD_REASON_CODE\":\"1\"," &
"\n    \"AC_FLD_PURCHASE_SOURCE\" : \"Testes Oracle SAP\"," &
"\n    \"AC_FLD_TRANSACTION_ID\" : \"1\"," &
"\n    \"AC_FLD_AUTHORIZATION_NO\" : \"zEFkNK01J4SM96HNZEFKNKFEE0NV01ZY\"," &
"\n    \"AC_FLD_PARCELA\": 1," &
"\n    \"AC_FLD_CONTRACT_ID\": \"TP_ORACLE_SAP_004\"," &
"\n    \"AC_FLD_STR_COD_TERMINAL\": \"\"," &
"\n    \"PIN_FLD_PAYINFO\": [" &
"\n        {" &
"\n            \"PIN_FLD_POID\": \"0.0.0.1 /payinfo -1 0\"," &
"\n            \"AC_FLD_CC_INFO\": {" &
"\n                \"PIN_FLD_DEBIT_NUM\" : \"Hm90XC01JZ6D31KWHM90XCBBNT5ZMKV4\"," &
"\n                \"AC_FLD_IS_HUB\" : \"True\"," &
"\n                \"AC_FLD_CC_PROVIDER\" : \"12\"," &
"\n                \"AC_FLD_DEBIT_MASKED_NUM\" : \"8883\"" &
"\n            }" &
"\n        }" &
"\n    ]," &
"\n   \"PIN_FLD_PRODUCTS\":[" &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"Banda Larga\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"8023h000007FvRAAA0\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Fibra 600 Mega\"," &
"\n          \"CYCLE_FEE_AMT\":6057," &
"\n          \"CYCLE_START_T\":\"0\"," &
"\n          \"AC_FLD_COD_ANATEL\":\"SCM004\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"BL_600_OFFER\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\":1," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"BL_600MB\"," &
"\n          \"AC_FLD_VELOCITY\":\"600\"," &
"\n          \"AC_FLD_FIDELIZACAO\" : \"Fidelização Anual\"" &
"\n       }," &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"SVA\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"8023h000004Te39AAC\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Nio Expert Presencial\"," &
"\n          \"CYCLE_FEE_AMT\":1490," &
"\n          \"AC_FLD_COD_ANATEL\":\"Anatel_SVA\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"Fibra_SVA\"," &
"\n          \"AC_ID_OFERTA\":\"1\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\" : \"1\"," &
"\n          \"AC_FLD_FLAG_EMBARCADO\":\"Embarcado\"," &
"\n          \"AC_FLD_INCIDENCIA\":\"Banda Larga\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"EXP_CSA\"" &
"\n       }," &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"SVA\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"8023h000007NgtrAAC\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Nio News Estadão\"," &
"\n          \"CYCLE_FEE_AMT\":399," &
"\n          \"AC_FLD_COD_ANATEL\":\"Anatel_SVA\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"Fibra_SVA\"," &
"\n          \"AC_ID_OFERTA\":\"1\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\" : \"1\"," &
"\n          \"AC_FLD_FLAG_EMBARCADO\":\"Embarcado\"," &
"\n          \"AC_FLD_INCIDENCIA\":\"Banda Larga\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"SVA_NEWS_ESTD\"" &
"\n       }," &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"SVA\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"8023h000007InNPAA0\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Nio News Isto É\"," &
"\n          \"CYCLE_FEE_AMT\":1070," &
"\n          \"AC_FLD_COD_ANATEL\":\"Anatel_SVA\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"Fibra_SVA\"," &
"\n          \"AC_ID_OFERTA\":\"1\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\" : \"1\"," &
"\n          \"AC_FLD_FLAG_EMBARCADO\":\"Embarcado\"," &
"\n          \"AC_FLD_INCIDENCIA\":\"Banda Larga\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"SVA_NEWS_IE\"" &
"\n       }," &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"SVA\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"8023h000007IdJIAA0\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Nio News O Dia\"," &
"\n          \"CYCLE_FEE_AMT\":199," &
"\n          \"AC_FLD_COD_ANATEL\":\"Anatel_SVA\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"Fibra_SVA\"," &
"\n          \"AC_ID_OFERTA\":\"1\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\" : \"1\"," &
"\n          \"AC_FLD_FLAG_EMBARCADO\":\"Embarcado\"," &
"\n          \"AC_FLD_INCIDENCIA\":\"Banda Larga\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"SVA_NEWS_ODIA\"" &
"\n       }," &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"SVA\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"8023h000005Kk1CAAS\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Nio Notícias\"," &
"\n          \"CYCLE_FEE_AMT\":560," &
"\n          \"AC_FLD_COD_ANATEL\":\"Anatel_SVA\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"Fibra_SVA\"," &
"\n          \"AC_ID_OFERTA\":\"1\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\" : \"1\"," &
"\n          \"AC_FLD_FLAG_EMBARCADO\":\"Embarcado\"," &
"\n          \"AC_FLD_INCIDENCIA\":\"Banda Larga\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"OI_NTC\"" &
"\n       }," &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"Oi Play\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"8023h000007B810AAC\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Nio Play Básico\"," &
"\n          \"CYCLE_FEE_AMT\":0," &
"\n          \"AC_FLD_COD_ANATEL\":\"Anatel_SVA\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"Fibra_SVA\"," &
"\n          \"AC_ID_OFERTA\":\"1\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\" : \"1\"," &
"\n          \"AC_FLD_FLAG_EMBARCADO\":\"Embarcado\"," &
"\n          \"AC_FLD_INCIDENCIA\":\"Banda Larga\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"OI_PLY_BSC\"" &
"\n       }," &
"\n   {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"SVA\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"802U6000000kTX8IAM\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"1 Ponto Extra Wi-Fi 5\"," &
"\n          \"CYCLE_FEE_AMT\":2646," &
"\n          \"AC_FLD_COD_ANATEL\" : \"\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\" : \"1\"," &
"\n          \"AC_FLD_FLAG_EMBARCADO\":\"Avulso\"," &
"\n          \"AC_FLD_INCIDENCIA\":\"Banda Larga\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"FIBRAX_MESH_1AP\"" &
"\n       }," &
"\n   {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\" : \"VoIP\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\" : \"80288000006BmXOAA0\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Nio Fixo Fibra\"," &
"\n          \"CYCLE_FEE_AMT\":2990," &
"\n          \"AC_FLD_COD_ANATEL\" : \"STFC001_002_003\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\" : \"1\"," &
"\n          \"AC_FLD_FLAG_EMBARCADO\" : \"Avulso\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\" : \"VOIP_FIXOILIMITADO\"," &
"\n          \"AC_FLD_STR_COD_TERMINAL\" : \"3144806269\"" &
"\n       }," &
"\n   {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"SVOD\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"802U600000F9hy9IAB\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Paramount+\"," &
"\n          \"CYCLE_FEE_AMT\":0," &
"\n          \"AC_FLD_COD_ANATEL\":\"\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"BL_600_OFFER\"," &
"\n          \"AC_ID_OFERTA\":\"1\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\" : \"1\"," &
"\n          \"AC_FLD_FLAG_EMBARCADO\":\"\"," &
"\n          \"AC_FLD_INCIDENCIA\":\"Banda Larga\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"PRMNT_PLUS\"" &
"\n       }" &
"\n    ]" &
"\n }")

    Private Shared ReadOnly _boletoTemplate As JObject = JObject.Parse(
"{" &
"\n    \"PIN_FLD_POID\":\"0.0.0.1 /account 540919636 0\"," &
"\n    \"PIN_FLD_BILLINFO_OBJ\":\"0.0.0.1 /billinfo -1 0\"," &
"\n    \"PIN_FLD_PAY_TYPE\":2," &
"\n    \"AC_FLD_PROTOCOL_ID\": \"ORACLE_SAP_001\"," &
"\n    \"AC_FLD_NSU_CIELO\":\"12301\"," &
"\n    \"AC_FLD_REASON_CODE\":\"1\"," &
"\n    \"AC_FLD_PURCHASE_SOURCE\" : \"Testes Oracle SAP\"," &
"\n    \"AC_FLD_TRANSACTION_ID\":\"1234501\"," &
"\n    \"AC_FLD_AUTHORIZATION_NO\":\"1234501\"," &
"\n    \"AC_FLD_PARCELA\":1," &
"\n    \"AC_FLD_CONTRACT_ID\": \"TP_ORACLE_SAP_001\"," &
"\n    \"AC_FLD_STR_COD_TERMINAL\": \"\"," &
"\n    \"PIN_FLD_PAYINFO\":[" &
"\n       {" &
"\n          \"PIN_FLD_POID\":\"0.0.0.1 /payinfo -1 0\"," &
"\n          \"AC_FLD_PAYINFO_BOLETO\":{" &
"\n             \"AC_FLD_AGENT_ID\":\"0\"" &
"\n          }" &
"\n       }" &
"\n    ]," &
"\n    \"PIN_FLD_PRODUCTS\":[" &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"Banda Larga\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"8023h000007NwGvAAK\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Fibra 700 Mega\"," &
"\n          \"CYCLE_FEE_AMT\":7172," &
"\n          \"CYCLE_START_T\":\"0\"," &
"\n          \"AC_FLD_COD_ANATEL\":\"SCM004\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"BL_700_OFFER\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\":1," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"BL_700MB\"," &
"\n          \"AC_FLD_VELOCITY\":\"700\"" &
"\n       }," &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"SVA\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"802HZ000004apoJYAQ\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Nio Áudio Livros\"," &
"\n          \"CYCLE_FEE_AMT\":600," &
"\n          \"AC_FLD_COD_ANATEL\":\"Anatel_SVA\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"Fibra_SVA\"," &
"\n          \"AC_ID_OFERTA\":\"1\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\":1," &
"\n          \"AC_FLD_FLAG_EMBARCADO\":\"Embarcado\"," &
"\n          \"AC_FLD_INCIDENCIA\":\"Banda Larga\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"OI_AUD_LVR\"" &
"\n       }," &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"SVA\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"802HZ000006Jh6HYAS\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Nio Livros\"," &
"\n          \"CYCLE_FEE_AMT\":2060," &
"\n          \"AC_FLD_COD_ANATEL\":\"Anatel_SVA\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"Fibra_SVA\"," &
"\n          \"AC_ID_OFERTA\":\"1\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\":1," &
"\n          \"AC_FLD_FLAG_EMBARCADO\":\"Embarcado\"," &
"\n          \"AC_FLD_INCIDENCIA\":\"Banda Larga\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"OI_LVR\"" &
"\n       }," &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"SVA\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"8023h000004Te39AAC\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Nio Expert Presencial\"," &
"\n          \"CYCLE_FEE_AMT\":1490," &
"\n          \"AC_FLD_COD_ANATEL\":\"Anatel_SVA\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"Fibra_SVA\"," &
"\n          \"AC_ID_OFERTA\":\"1\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\":1," &
"\n          \"AC_FLD_FLAG_EMBARCADO\":\"Embarcado\"," &
"\n          \"AC_FLD_INCIDENCIA\":\"Banda Larga\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"EXP_CSA\"" &
"\n       }," &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"SVA\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"8023h000007NgtrAAC\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Nio News Estadão\"," &
"\n          \"CYCLE_FEE_AMT\":399," &
"\n          \"AC_FLD_COD_ANATEL\":\"Anatel_SVA\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"Fibra_SVA\"," &
"\n          \"AC_ID_OFERTA\":\"1\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\":1," &
"\n          \"AC_FLD_FLAG_EMBARCADO\":\"Embarcado\"," &
"\n          \"AC_FLD_INCIDENCIA\":\"Banda Larga\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"SVA_NEWS_ESTD\"" &
"\n       }," &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"SVA\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"8023h000007InNPAA0\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Nio News Isto É\"," &
"\n          \"CYCLE_FEE_AMT\":1070," &
"\n          \"AC_FLD_COD_ANATEL\":\"Anatel_SVA\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"Fibra_SVA\"," &
"\n          \"AC_ID_OFERTA\":\"1\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\":1," &
"\n          \"AC_FLD_FLAG_EMBARCADO\":\"Embarcado\"," &
"\n          \"AC_FLD_INCIDENCIA\":\"Banda Larga\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"SVA_NEWS_IE\"" &
"\n       }," &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"SVA\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"8023h000007IdJIAA0\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Nio News O Dia\"," &
"\n          \"CYCLE_FEE_AMT\":199," &
"\n          \"AC_FLD_COD_ANATEL\":\"Anatel_SVA\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"Fibra_SVA\"," &
"\n          \"AC_ID_OFERTA\":\"1\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\":1," &
"\n          \"AC_FLD_FLAG_EMBARCADO\":\"Embarcado\"," &
"\n          \"AC_FLD_INCIDENCIA\":\"Banda Larga\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"SVA_NEWS_ODIA\"" &
"\n       }," &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"SVA\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"8023h000005Kk1CAAS\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Nio Notícias\"," &
"\n          \"CYCLE_FEE_AMT\":560," &
"\n          \"AC_FLD_COD_ANATEL\":\"Anatel_SVA\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"Fibra_SVA\"," &
"\n          \"AC_ID_OFERTA\":\"1\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\":1," &
"\n          \"AC_FLD_FLAG_EMBARCADO\":\"Embarcado\"," &
"\n          \"AC_FLD_INCIDENCIA\":\"Banda Larga\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"\"" &
"\n       }," &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"SVA\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"8023h000007B810AAC\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Nio Play Básico\"," &
"\n          \"CYCLE_FEE_AMT\":0," &
"\n          \"AC_FLD_COD_ANATEL\":\"Anatel_SVA\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"Fibra_SVA\"," &
"\n          \"AC_ID_OFERTA\":\"1\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\":1," &
"\n          \"AC_FLD_FLAG_EMBARCADO\":\"Embarcado\"," &
"\n          \"AC_FLD_INCIDENCIA\":\"Banda Larga\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"OI_PLY_BSC\"" &
"\n       }" &
"\n    ]" &
"\n }")

    Private Shared ReadOnly _dacTemplate As JObject = JObject.Parse(
"{" &
"\n   \"PIN_FLD_POID\":\"0.0.0.1 /account 540926405 0\"," &
"\n   \"PIN_FLD_BILLINFO_OBJ\":\"0.0.0.1 /billinfo -1 0\"," &
"\n   \"PIN_FLD_PAY_TYPE\": 3," &
"\n    \"AC_FLD_PROTOCOL_ID\": \"ORACLE_SAP_006\"," &
"\n   \"AC_FLD_NSU_CIELO\":\"123\"," &
"\n   \"AC_FLD_REASON_CODE\":\"1\"," &
"\n   \"AC_FLD_PURCHASE_SOURCE\" : \"Testes Oracle SAP\"," &
"\n   \"AC_FLD_TRANSACTION_ID\":\"618648749606489351\"," &
"\n   \"AC_FLD_AUTHORIZATION_NO\":\"12345\"," &
"\n   \"AC_FLD_PARCELA\":1," &
"\n    \"AC_FLD_CONTRACT_ID\": \"TP_ORACLE_SAP_006\"," &
"\n   \"AC_FLD_REUSO_FLAG\":0," &
"\n    \"AC_FLD_STR_COD_TERMINAL\": \"\"," &
"\n   \"PIN_FLD_PAYINFO\":[" &
"\n   {" &
"\n       \"PIN_FLD_POID\": \"0.0.0.1 /payinfo -1 0\"," &
"\n       \"AC_FLD_DACC_INFO\": " &
"\n       {" &
"\n           \"AC_FLD_BANK_NO\": \"341\"," &
"\n           \"AC_FLD_AGENCIA_DACC\": \"7077\"," &
"\n           \"PIN_FLD_CONTA_DEBITO\": \"067869-9\"" &
"\n        }" &
"\n    }   ]," &
"\n    \"PIN_FLD_PRODUCTS\":[" &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"Banda Larga\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"80288000003iJWiAAM\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Fibra 1 Giga\"," &
"\n          \"CYCLE_FEE_AMT\":45520," &
"\n          \"CYCLE_START_T\":\"0\"," &
"\n          \"AC_FLD_COD_ANATEL\":\"SCM004\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"BL_1000_OFFER\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\":1," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"BL_1000MB\"," &
"\n          \"AC_FLD_VELOCITY\":\"1000\"," &
"\n          \"AC_FLD_FIDELIZACAO\" : \"Fidelização Anual\"" &
"\n       }," &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"SVA\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"8023h000004Te39AAC\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Nio Expert Presencial\"," &
"\n          \"CYCLE_FEE_AMT\":1490," &
"\n          \"AC_FLD_COD_ANATEL\":\"Anatel_SVA\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"Fibra_SVA\"," &
"\n          \"AC_ID_OFERTA\":\"1\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\" : \"1\"," &
"\n          \"AC_FLD_FLAG_EMBARCADO\":\"Embarcado\"," &
"\n          \"AC_FLD_INCIDENCIA\":\"Banda Larga\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"EXP_CSA\"" &
"\n       }," &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"SVA\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"8023h000007NgtrAAC\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Nio News Estadão\"," &
"\n          \"CYCLE_FEE_AMT\":399," &
"\n          \"AC_FLD_COD_ANATEL\":\"Anatel_SVA\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"Fibra_SVA\"," &
"\n          \"AC_ID_OFERTA\":\"1\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\" : \"1\"," &
"\n          \"AC_FLD_FLAG_EMBARCADO\":\"Embarcado\"," &
"\n          \"AC_FLD_INCIDENCIA\":\"Banda Larga\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"SVA_NEWS_ESTD\"" &
"\n       }," &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"SVA\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"8023h000007InNPAA0\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Nio News Isto É\"," &
"\n          \"CYCLE_FEE_AMT\":1070," &
"\n          \"AC_FLD_COD_ANATEL\":\"Anatel_SVA\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"Fibra_SVA\"," &
"\n          \"AC_ID_OFERTA\":\"1\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\" : \"1\"," &
"\n          \"AC_FLD_FLAG_EMBARCADO\":\"Embarcado\"," &
"\n          \"AC_FLD_INCIDENCIA\":\"Banda Larga\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"SVA_NEWS_IE\"" &
"\n       }," &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"SVA\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"8023h000007IdJIAA0\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Nio News O Dia\"," &
"\n          \"CYCLE_FEE_AMT\":199," &
"\n          \"AC_FLD_COD_ANATEL\":\"Anatel_SVA\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"Fibra_SVA\"," &
"\n          \"AC_ID_OFERTA\":\"1\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\" : \"1\"," &
"\n          \"AC_FLD_FLAG_EMBARCADO\":\"Embarcado\"," &
"\n          \"AC_FLD_INCIDENCIA\":\"Banda Larga\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"SVA_NEWS_ODIA\"" &
"\n       }," &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"SVA\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"8023h000005Kk1CAAS\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Nio Notícias\"," &
"\n          \"CYCLE_FEE_AMT\":560," &
"\n          \"AC_FLD_COD_ANATEL\":\"Anatel_SVA\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"Fibra_SVA\"," &
"\n          \"AC_ID_OFERTA\":\"1\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\" : \"1\"," &
"\n          \"AC_FLD_FLAG_EMBARCADO\":\"Embarcado\"," &
"\n          \"AC_FLD_INCIDENCIA\":\"Banda Larga\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"OI_NTC\"" &
"\n       }," &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"Oi Play\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"8023h000007B810AAC\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Nio Play Básico\"," &
"\n          \"CYCLE_FEE_AMT\":0," &
"\n          \"AC_FLD_COD_ANATEL\":\"Anatel_SVA\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"Fibra_SVA\"," &
"\n          \"AC_ID_OFERTA\":\"1\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\" : \"1\"," &
"\n          \"AC_FLD_FLAG_EMBARCADO\":\"Embarcado\"," &
"\n          \"AC_FLD_INCIDENCIA\":\"Banda Larga\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"OI_PLY_BSC\"" &
"\n       }," &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"SVA\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"802U6000000YuhbIAC\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"1 Ponto Extra Wi-Fi 6 FTTR\"," &
"\n          \"CYCLE_FEE_AMT\":2500," &
"\n          \"AC_FLD_COD_ANATEL\" : \"\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\" : \"1\"," &
"\n          \"AC_FLD_FLAG_EMBARCADO\":\"Embarcado\"," &
"\n          \"AC_FLD_INCIDENCIA\":\"Banda Larga\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"FIBRAX_FTTR_1AP\"" &
"\n       }," &
"\n       {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\":\"SVOD\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\":\"802U6000008PsYPIA0\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Max\"," &
"\n          \"CYCLE_FEE_AMT\":0," &
"\n          \"AC_FLD_COD_ANATEL\":\"\"," &
"\n          \"AC_FLD_ID_OFERTA\":\"BL_1000_OFFER\"," &
"\n          \"AC_ID_OFERTA\":\"1\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\" : \"1\"," &
"\n          \"AC_FLD_FLAG_EMBARCADO\":\"\"," &
"\n          \"AC_FLD_INCIDENCIA\":\"Banda Larga\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\":\"HBO_MAX\"" &
"\n       }," &
"\n   {" &
"\n          \"PIN_FLD_PRODUCT_OBJ\" : \"VoIP\"," &
"\n          \"AC_FLD_CRM_PRODUCT_ID\" : \"80288000006BmXOAA0\"," &
"\n          \"AC_FLD_CRM_PRODUCT_DESCR\":\"Nio Fixo Fibra\"," &
"\n          \"CYCLE_FEE_AMT\":2990," &
"\n          \"AC_FLD_COD_ANATEL\" : \"STFC001_002_003\"," &
"\n          \"AC_FLD_FLAG_VISIBILITY\" : \"1\"," &
"\n          \"AC_FLD_FLAG_EMBARCADO\" : \"Avulso\"," &
"\n          \"AC_FLD_CAT_PRODUCT_ID\" : \"VOIP_FIXOILIMITADO\"," &
"\n          \"AC_FLD_STR_COD_TERMINAL\" : \"3164611919\"" &
"\n       }" &
"\n    ]" &
"\n }")

    Private Shared Function GetTemplate(tipo As PayType) As JObject
        Select Case tipo
            Case PayType.CreditCard
                Return _creditCardTemplate
            Case PayType.Boleto
                Return _boletoTemplate
            Case PayType.DAC
                Return _dacTemplate
            Case Else
                Return _creditCardTemplate
        End Select
    End Function

    ' ===== Telemetría/OUT =====
    Public Property LastRequestJson As String
    Public Property LastResponseBody As String
    Public Property LastHttpStatus As Integer?
    Public Property ErrorMessage As String
    Public Property OnOut As Action(Of String)

    Private Sub OUT(line As String)
        Try
            If OnOut IsNot Nothing Then OnOut.Invoke(line)
        Catch
        End Try
    End Sub

    Private Sub LogBlock(header As String,
                         body As String,
                         Optional scope As String = "PURCHASE",
                         Optional isJson As Boolean = True)
        If Not String.IsNullOrWhiteSpace(header) Then
            OUT(header)
            LogInfoToLogger(scope, header)
        End If
        If isJson Then
            LogJsonToLogger(scope, body)
        Else
            LogMultiline(body)
            LogInfoToLogger(scope, body)
        End If
    End Sub

    Private Sub LogMultiline(body As String)
        If String.IsNullOrWhiteSpace(body) Then Return

        Dim normalized As String = body.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf)
        Dim lines = normalized.Split(New String() {vbLf}, StringSplitOptions.None)
        For Each line In lines
            OUT(line)
        Next
    End Sub

    Private Shared Function FormatJsonForLog(raw As String) As String
        If String.IsNullOrWhiteSpace(raw) Then Return raw
        Try
            Dim token As JToken = JToken.Parse(raw)
            Return token.ToString(Formatting.Indented)
        Catch
            Return raw
        End Try
    End Function

    Private Sub LogInfoToLogger(scope As String, message As String)
        If _logger Is Nothing OrElse String.IsNullOrWhiteSpace(message) Then Return
        Try
            _logger.LogData(message, scope)
        Catch
        End Try
    End Sub

    Private Sub LogJsonToLogger(scope As String, json As String)
        If _logger Is Nothing OrElse String.IsNullOrWhiteSpace(json) Then Return
        Try
            Dim pretty As String = FormatJsonForLog(json)
            _logger.LogJson(pretty, scope)
        Catch
        End Try
    End Sub

    ' ===== API =====
    Private _logger As IAppLogger

    Public Property Logger As IAppLogger
        Get
            Return _logger
        End Get
        Set(value As IAppLogger)
            _logger = value
        End Set
    End Property

    Public Async Function ComprarAsync(accountPoid As String,
                                       tipo As PayType?,
                                       Optional persist As Boolean = True,
                                       Optional payTypeOverride As Integer? = Nothing) As Task(Of CompraProductosResult)

        Dim r As New CompraProductosResult With {.AccountPoid = accountPoid}

        Try
            Dim poid As String = NormalizarAccountPoid(accountPoid)
            If String.IsNullOrWhiteSpace(poid) Then
                ErrorMessage = "AccountPoid inválido."
                Dim msg As String = "[PURCHASE][ERROR] " & ErrorMessage
                OUT(msg)
                LogInfoToLogger("PURCHASE_ERROR", msg)
                Return r
            End If

            ' 1) Protocolo/Contrato y Terminal
            Dim protocol As String = GenerateUniqueProtocolId(PROTOCOL_PREFIX)
            Dim contractId As String = CONTRACT_PREFIX & protocol
            Dim terminal As String = GenerarTerminalUnico()

            ' 2) Pay type
            Dim selectedPayType As PayType
            Dim randomPayType As Boolean = False
            If tipo.HasValue AndAlso [Enum].IsDefined(GetType(PayType), tipo.Value) Then
                selectedPayType = tipo.Value
            Else
                selectedPayType = GetRandomPayType()
                randomPayType = True
            End If

            Dim pinPayType As Integer = If(payTypeOverride.HasValue, payTypeOverride.Value,
                                           CInt(selectedPayType))

            ' 3) JSON exacto
            Dim payload As JObject = BuildPurchasePayload(poid, protocol, contractId, terminal, pinPayType, selectedPayType)
            Dim json As String = payload.ToString(Formatting.None)

            LastRequestJson = json
            Dim accountLine As String = "[PURCHASE] AccountPoid=" & poid
            OUT(accountLine)
            LogInfoToLogger("PURCHASE", accountLine)

            Dim protocolLine As String = "[PURCHASE] Protocol=" & protocol & " Contract=" & contractId & " Terminal=" & terminal
            OUT(protocolLine)
            LogInfoToLogger("PURCHASE", protocolLine)

            Dim payTypeLine As String = String.Format("[PURCHASE] PayType={0} (PIN={1}){2}",
                                                      selectedPayType.ToString(),
                                                      pinPayType,
                                                      If(randomPayType, " [ALEATORIO]", String.Empty))
            OUT(payTypeLine)
            LogInfoToLogger("PURCHASE", payTypeLine)

            LogBlock("=== PURCHASE REQUEST (ver Log_Debug) ===", LastRequestJson, "PURCHASE_REQUEST")

            If Not persist Then
                r.Success = True
                r.ProtocolId = protocol
                r.ContractId = contractId
                r.Terminal = terminal
                Dim dryRunMsg As String = "[PURCHASE][DRY-RUN] persist=False, POST omitido."
                OUT(dryRunMsg)
                LogInfoToLogger("PURCHASE_DRYRUN", dryRunMsg)
                Return r
            End If

            ' 4) POST
            Dim endpoint As String = BASE_URL.TrimEnd("/"c) & PATH_PURCHASE
            LogBlock("=== PURCHASE URL ===", endpoint, "PURCHASE_URL", isJson:=False)
            Using req As New HttpRequestMessage(HttpMethod.Post, endpoint)
                req.Headers.Accept.Clear()
                req.Headers.Accept.ParseAdd("application/json")
                req.Content = New StringContent(json, Encoding.UTF8, "application/json")

                Using resp = Await _http.SendAsync(req).ConfigureAwait(False)
                    Dim body As String = Await resp.Content.ReadAsStringAsync().ConfigureAwait(False)
                    LastHttpStatus = CInt(resp.StatusCode)
                    LastResponseBody = body

                    Dim reason As String = If(String.IsNullOrWhiteSpace(resp.ReasonPhrase), resp.StatusCode.ToString(), resp.ReasonPhrase)
                    Dim statusLine As String = String.Format("Status: {0} {1}", LastHttpStatus.GetValueOrDefault(), reason)
                    OUT(statusLine)
                    LogInfoToLogger("PURCHASE_HTTP", statusLine)

                    LogBlock("=== PURCHASE RESPONSE (ver Log_Debug) ===", body, "PURCHASE_RESPONSE")

                    ' 5) Validaciones posteriores a la compra
                    Dim okProtocolo As Boolean = False
                    For attempt As Integer = 1 To VALIDATION_MAX_ATTEMPTS
                        okProtocolo = ExisteCompraEnBdPorProtocolo(protocol)
                        Dim baseLine As String = String.Format("[PURCHASE][VALIDATION][PROTOCOLO] intento {0}: {1}",
                                                                 attempt,
                                                                 If(okProtocolo, "OK", "SIN REGISTRO"))
                        If okProtocolo OrElse attempt = VALIDATION_MAX_ATTEMPTS Then
                            OUT(baseLine)
                            LogInfoToLogger("PURCHASE_VALIDATION", baseLine)
                        Else
                            Dim retryLine As String = baseLine & ". Reintentando en 2s..."
                            OUT(retryLine)
                            LogInfoToLogger("PURCHASE_VALIDATION", retryLine)
                            Await Task.Delay(VALIDATION_DELAY).ConfigureAwait(False)
                        End If

                        If okProtocolo Then Exit For
                    Next

                    Dim okProductos As Boolean = False
                    For attempt As Integer = 1 To VALIDATION_MAX_ATTEMPTS
                        Dim consulta = ConsultarProductosActivosPorAccount(poid)
                        Dim headerProductos As String = String.Format("=== PURCHASE PRODUCTS (query) intento {0} ===", attempt)
                        LogBlock(headerProductos, consulta.Tabla, "PURCHASE_PRODUCTS", isJson:=False)

                        Dim baseProductos As String = String.Format("[PURCHASE][VALIDATION][PRODUCTOS] intento {0}: {1}",
                                                                    attempt,
                                                                    If(consulta.TieneProductoActivo, "OK", "SIN PRODUCTOS ACTIVOS"))

                        If consulta.TieneProductoActivo OrElse consulta.HuboError OrElse attempt = VALIDATION_MAX_ATTEMPTS Then
                            OUT(baseProductos)
                            LogInfoToLogger("PURCHASE_VALIDATION", baseProductos)
                        Else
                            Dim retryProductos As String = baseProductos & ". Reintentando en 2s..."
                            OUT(retryProductos)
                            LogInfoToLogger("PURCHASE_VALIDATION", retryProductos)
                            Await Task.Delay(VALIDATION_DELAY).ConfigureAwait(False)
                        End If

                        If consulta.HuboError Then
                            Exit For
                        End If

                        If consulta.TieneProductoActivo Then
                            okProductos = True
                            Exit For
                        End If
                    Next

                    r.Success = okProtocolo AndAlso okProductos

                    r.ProtocolId = protocol
                    r.ContractId = contractId
                    r.Terminal = terminal
                    r.HttpStatus = LastHttpStatus
                    r.RawBody = LastResponseBody

                    If Not r.Success Then
                        If Not okProtocolo Then
                            ErrorMessage = "No se encontró confirmación de la compra en la bitácora."
                        ElseIf Not okProductos Then
                            ErrorMessage = "No se hallaron productos activos para la cuenta tras la compra."
                        End If
                    End If


                End Using
            End Using

        Catch ex As Exception
            ErrorMessage = ex.Message
            OUT("[PURCHASE][ERROR] " & ErrorMessage)
            If _logger IsNot Nothing Then
                Try
                    _logger.LogError(ErrorMessage, ex, New With {.AccountPoid = accountPoid})
                Catch
                End Try
            End If
            r.Success = False
            r.HttpStatus = LastHttpStatus
            r.RawBody = LastResponseBody
        End Try

        Return r
    End Function

    Private Function BuildPurchasePayload(accountPoid As String,
                                          protocolId As String,
                                          contractId As String,
                                          terminal As String,
                                          pinPayType As Integer,
                                          tipo As PayType) As JObject
        Dim template As JObject = CType(GetTemplate(tipo).DeepClone(), JObject)
        template("PIN_FLD_POID") = accountPoid
        template("AC_FLD_PROTOCOL_ID") = protocolId
        template("AC_FLD_CONTRACT_ID") = contractId
        template("AC_FLD_STR_COD_TERMINAL") = terminal
        template("PIN_FLD_PAY_TYPE") = pinPayType
        Return template
    End Function

    ' ====== Validación mínima ======
    Private Function ExisteCompraEnBdPorProtocolo(protocolId As String) As Boolean
        Try
            Dim sql As String =
"SELECT CASE WHEN EXISTS (
    SELECT 1 FROM pin.ac_interface_log_t
     WHERE input_json LIKE '%' || :p || '%'
) THEN 1 ELSE 0 END AS existe FROM dual"
            Dim v As Integer = _db.ExecuteScalar(Of Integer)(sql,
                New Dictionary(Of String, Object) From {{":p", protocolId}}, 20)
            Return (v = 1)
        Catch
            Return False
        End Try
    End Function

    ' ====== Secuenciador/Terminal ======
    Private Function GenerateUniqueProtocolId(prefix As String) As String
        Dim baseMax As Integer = GetMaxSuffixFromDb(prefix)
        If baseMax < 0 Then baseMax = 0
        Dim trySuffix As Integer = baseMax + 1
        Dim attempts As Integer = 0
        While attempts < 300
            Dim candidate As String = prefix & trySuffix.ToString("0000")
            If Not ProtocoloExiste(candidate) Then
                Dim seqLine As String = "[SEQ][PURCHASE] next unique → " & candidate
                OUT(seqLine)
                LogInfoToLogger("PURCHASE_SEQ", seqLine)
                Return candidate
            End If
            trySuffix += 1
            attempts += 1
        End While
        Return prefix & DateTime.Now.ToString("mmss")
    End Function

    Private Function GetMaxSuffixFromDb(prefix As String) As Integer
        Dim maxSuffix As Integer = -1
        Try
            Dim sql1 As String =
"SELECT NVL(MAX(TO_NUMBER(REGEXP_SUBSTR(TRIM(ac_protocol_id),'([0-9]{4})$'))),0)
   FROM pin.ac_protocol_t
  WHERE ac_protocol_id LIKE :pfx || '%'"
            Dim v1 As Integer = _db.ExecuteScalar(Of Integer)(sql1, New Dictionary(Of String, Object) From {{":pfx", prefix}}, 15)
            maxSuffix = Math.Max(maxSuffix, v1)

            Dim sql2 As String =
"SELECT NVL(MAX(TO_NUMBER(REGEXP_SUBSTR(TRIM(contract_id),'([0-9]{4})$'))),0)
   FROM pin.ac_profile_account_t
  WHERE contract_id LIKE :pfx || '%'"
            Dim v2 As Integer = _db.ExecuteScalar(Of Integer)(sql2, New Dictionary(Of String, Object) From {{":pfx", prefix}}, 15)
            maxSuffix = Math.Max(maxSuffix, v2)
        Catch ex As Exception
            Dim seqErr As String = "[SEQ][PURCHASE][ERR] " & ex.Message
            OUT(seqErr)
            LogInfoToLogger("PURCHASE_SEQ_ERROR", seqErr)
            Return -1
        End Try
        Return maxSuffix
    End Function

    Private Function ProtocoloExiste(candidate As String) As Boolean
        Try
            Dim sql As String =
"SELECT CASE WHEN EXISTS (SELECT 1 FROM pin.ac_protocol_t WHERE TRIM(ac_protocol_id) = :cand)
              OR EXISTS (SELECT 1 FROM pin.ac_profile_account_t WHERE TRIM(contract_id) = :cand)
           THEN 1 ELSE 0 END AS existe
  FROM dual"
            Dim v As Integer = _db.ExecuteScalar(Of Integer)(sql,
                New Dictionary(Of String, Object) From {{":cand", candidate}}, 15)
            Return (v = 1)
        Catch
            LogInfoToLogger("PURCHASE_SEQ_CHECK", "[SEQ][PURCHASE] ProtocoloExiste fallback True")
            Return True
        End Try
    End Function

    Private Shared ReadOnly _rnd As New Random()

    Private Shared Function GetRandomPayType() As PayType
        Dim values As Array = [Enum].GetValues(GetType(PayType))
        Dim index As Integer = _rnd.Next(values.Length)
        Return CType(values.GetValue(index), PayType)
    End Function

    Private Function GenerarTerminalUnico() As String
        Dim tries As Integer = 0
        While tries < 200
            ' 10 dígitos
            Dim t As String = _rnd.Next(300000000, 399999999).ToString() & _rnd.Next(10, 99).ToString()
            If Not TerminalExiste(t) Then Return t
            tries += 1
        End While
        Return _rnd.Next(1000000000, Integer.MaxValue).ToString()
    End Function

    Private Function TerminalExiste(terminal As String) As Boolean
        Try
            Dim sql As String =
"SELECT CASE WHEN EXISTS (
  SELECT 1 FROM pin.ac_interface_log_t
   WHERE input_json LIKE '%' || :t || '%'
) THEN 1 ELSE 0 END AS e FROM dual"
            Dim v As Integer = _db.ExecuteScalar(Of Integer)(sql,
                New Dictionary(Of String, Object) From {{":t", terminal}}, 15)
            Return (v = 1)
        Catch
            LogInfoToLogger("PURCHASE_TERMINAL_CHECK", "[PURCHASE] TerminalExiste fallback False")
            Return False
        End Try
    End Function

    Private Function NormalizarAccountPoid(poid As String) As String
        If String.IsNullOrWhiteSpace(poid) Then Return ""
        Dim s As String = poid.Trim()
        If s.IndexOf("/account", StringComparison.OrdinalIgnoreCase) >= 0 Then
            Return s
        End If
        Dim idNum As Long
        If Long.TryParse(s, idNum) Then
            Return "0.0.0.1 /account " & idNum.ToString() & " 0"
        End If
        Return s
    End Function

    Private Function ConsultarProductosActivosPorAccount(accountPoid As String) As ConsultaProductosResult
        Dim result As New ConsultaProductosResult() With {.Tabla = "(sin registros)"}
        Dim accountObjId As Long?
        Try
            accountObjId = ExtraerAccountObjId(accountPoid)
        Catch ex As Exception
            Dim parseErr As String = "[PURCHASE][VALIDATION][PRODUCTOS] Error al interpretar AccountPoid: " & ex.Message
            OUT(parseErr)
            LogInfoToLogger("PURCHASE_VALIDATION", parseErr)
            result.HuboError = True
            Return result
        End Try

        If Not accountObjId.HasValue Then
            Dim msg As String = "[PURCHASE][VALIDATION][PRODUCTOS] AccountObjId no disponible para validar."
            OUT(msg)
            LogInfoToLogger("PURCHASE_VALIDATION", msg)
            Return result
        End If

        Try
            Dim sql As String = String.Join(vbLf, {
                "SELECT",
                "DECODE (E.STATUS, 10100, 'ATIVO', 10102,'SUSPENSO', 10103,'CANCELADO') AS STATUS,",
                "A.ACCOUNT_OBJ_ID0 AS ACCOUNT,",
                "TO_CHAR((TIMESTAMP '1970-01-01 00:00:00 +00:00' + NUMTODSINTERVAL(A.CREATED_T,'SECOND')) AT TIME ZONE 'GMT','DD.MM.YYYY HH24:MI:SS') AS DT_COMPRA,",
                "A.STATUS AS STATUS_PRODUTO,",
                "A.CYCLE_FEE_AMT / 100 AS VALOR,",
                "A.SERVICE_OBJ_ID0,",
                "A.SERVICE_OBJ_TYPE AS CLASSE,",
                "B.CRM_PRODUCT_DESCR,",
                "B.FLAG_EMBARCADO,",
                "B.CRM_PRODUCT_ID,",
                "B.CAT_PRODUCT_ID,",
                "B.FLAG_INVOL,",
                "B.COD_ANATEL",
                "FROM PIN.PURCHASED_PRODUCT_T A",
                "JOIN PIN.AC_PURCHASED_PRODUCT_T B ON B.PURCHASED_PRODUCT_OBJ_ID0 = A.POID_ID0",
                "JOIN PIN.SERVICE_T E ON A.SERVICE_OBJ_ID0 = E.POID_ID0",
                "WHERE A.ACCOUNT_OBJ_ID0 = :acctId",
                "  AND E.STATUS = 10100",
                "ORDER BY A.CREATED_T DESC"
            })

            Dim dt As DataTable = _db.ExecuteDataTable(sql,
                New Dictionary(Of String, Object) From {{":acctId", accountObjId.Value}}, 40)

            result.Tabla = FormatearTabla(dt)

            If dt Is Nothing OrElse dt.Rows.Count = 0 Then Return result

            For Each row As DataRow In dt.Rows
                Dim statusServicio As String = Convert.ToString(row("STATUS")).Trim()
                Dim productoActivo As Boolean = EvaluarStatusProducto(row)
                If String.Equals(statusServicio, "ATIVO", StringComparison.OrdinalIgnoreCase) AndAlso productoActivo Then
                    result.TieneProductoActivo = True
                    Exit For
                End If
            Next
        Catch ex As Exception
            Dim err As String = "[PURCHASE][VALIDATION][PRODUCTOS] " & ex.Message
            OUT(err)
            LogInfoToLogger("PURCHASE_VALIDATION", err)
            result.HuboError = True
        End Try

        Return result
    End Function

    Private Shared Function EvaluarStatusProducto(row As DataRow) As Boolean
        If row Is Nothing Then Return False
        Try
            If row.Table.Columns.Contains("STATUS_PRODUTO") Then
                Dim raw = row("STATUS_PRODUTO")
                If raw Is Nothing OrElse raw Is DBNull.Value Then Return False
                Dim statusInt As Integer
                If Integer.TryParse(Convert.ToString(raw).Trim(), statusInt) Then
                    Return (statusInt = 1)
                End If
                Dim statusText As String = Convert.ToString(raw).Trim()
                Return statusText.Equals("ATIVO", StringComparison.OrdinalIgnoreCase)
            End If
        Catch
        End Try
        Return False
    End Function

    Private Shared Function FormatearTabla(dt As DataTable) As String
        If dt Is Nothing OrElse dt.Columns.Count = 0 Then
            Return "(sin registros)"
        End If

        Dim cols = dt.Columns.Cast(Of DataColumn)().ToList()
        Dim widths As New List(Of Integer)(cols.Count)
        For Each col In cols
            Dim maxLen As Integer = col.ColumnName.Length
            For Each row As DataRow In dt.Rows
                Dim value As String = Convert.ToString(row(col)).Trim()
                If value.Length > maxLen Then maxLen = value.Length
            Next
            widths.Add(Math.Min(Math.Max(maxLen, 3), 120))
        Next

        Dim sb As New StringBuilder()
        For i As Integer = 0 To cols.Count - 1
            Dim header As String = cols(i).ColumnName
            sb.Append(header.PadRight(widths(i))).Append("  ")
        Next
        sb.AppendLine()

        For i As Integer = 0 To cols.Count - 1
            sb.Append(New String("-"c, widths(i))).Append("  ")
        Next
        sb.AppendLine()

        For Each row As DataRow In dt.Rows
            For i As Integer = 0 To cols.Count - 1
                Dim val As String = Convert.ToString(row(cols(i))).Trim()
                If val.Length > widths(i) Then
                    val = val.Substring(0, widths(i) - 1) & "…"
                End If
                sb.Append(val.PadRight(widths(i))).Append("  ")
            Next
            sb.AppendLine()
        Next

        Return sb.ToString().TrimEnd()
    End Function

    Private Shared Function ExtraerAccountObjId(accountPoid As String) As Long?
        If String.IsNullOrWhiteSpace(accountPoid) Then Return Nothing

        Dim cleaned As String = accountPoid.Trim()

        Dim idx As Integer = cleaned.IndexOf("/account", StringComparison.OrdinalIgnoreCase)
        If idx >= 0 Then
            Dim tail As String = cleaned.Substring(idx + 8).Trim()
            Dim parts = tail.Split(New Char() {" "c, "/"c}, StringSplitOptions.RemoveEmptyEntries)
            For Each part In parts
                Dim value As Long
                If Long.TryParse(part, value) Then
                    Return value
                End If
            Next
        End If

        Dim numericOnly As String = New String(cleaned.Where(AddressOf Char.IsDigit).ToArray())
        Dim parsed As Long
        If Not String.IsNullOrWhiteSpace(numericOnly) AndAlso Long.TryParse(numericOnly, parsed) Then
            Return parsed
        End If

        Return Nothing
    End Function


    Private Class ConsultaProductosResult
        Public Property TieneProductoActivo As Boolean
        Public Property Tabla As String
        Public Property HuboError As Boolean
    End Class


End Class
