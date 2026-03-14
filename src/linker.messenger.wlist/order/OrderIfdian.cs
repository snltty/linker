using linker.libs;
using linker.libs.extends;
using System.Net.Http.Json;
using System.Text.Json;

namespace linker.messenger.wlist.order
{
    public sealed class OrderIfdian : IOrder
    {
        public string Type => "ifdian";

        private const string api = "https://ifdian.net/api/open/query-order";

        private readonly IWhiteListServerStore whiteListServerStore;
        public OrderIfdian(IWhiteListServerStore whiteListServerStore)
        {
            this.whiteListServerStore = whiteListServerStore;
        }

        public async Task<string> ExecuteAsync(string userid, string machineId, string type, string tradeNo)
        {
            if (string.IsNullOrWhiteSpace(whiteListServerStore.Config.Value))
            {
                return "Ifdian userid and token not found";
            }
            string[] arr = whiteListServerStore.Config.Value.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            if (arr.Length != 2)
            {
                return "Ifdian userid and token config error";
            }

            WhiteListInfo white = await whiteListServerStore.Get(tradeNo).ConfigureAwait(false);
            if (white != null)
            {
                return "Ifdian order is already in use";
            }


            (IfdianOrderData data, string error) = await Get(arr[0], arr[1], tradeNo).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(error) == false) return $"Ifdian query order error {error}";
            if (data == null || data.List.Length == 0) return "Ifdian order not found";

            try
            {
                await whiteListServerStore.Add(new WhiteListInfo
                {
                    TradeNo = data.List[0].Out_trade_no,
                    UserId = userid,
                    MachineId = machineId,
                    Remark = data.List[0].Remark,
                    AddTime = DateTime.Now,
                    Name = data.List[0].User_name,
                    Nodes = ["*"],
                    Type = type,
                    UseTime = DateTime.Now,
                    EndTime = DateTime.Now.AddMonths(data.List[0].Month),
                    Bandwidth = int.Parse(data.List[0].Plan_title),
                });
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return "success";
        }

        public bool CheckEnabled()
        {
            return string.IsNullOrWhiteSpace(whiteListServerStore.Config.Value) == false
                && whiteListServerStore.Config.Value.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries).Length == 2;
        }

        private async Task<(IfdianOrderData data, string error)> Get(string userid, string token, string tradeNo)
        {
            try
            {
                string param = JsonSerializer.Serialize(new { page = 1, per_page = 1, out_trade_no = tradeNo });
                string ts = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();

                using HttpClient client = new HttpClient();
                JsonContent json = JsonContent.Create(new Dictionary<string, object>
                {
                    { "user_id" , userid },
                    { "ts" , ts },
                    { "sign" ,  $"{token}params{param}ts{ts}user_id{userid}".Md5() },
                    { "params" ,  param},
                });
                HttpResponseMessage resp = await client.PostAsync(api, json).ConfigureAwait(false);
                if (resp.IsSuccessStatusCode)
                {
                    string str = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    IfdianOrderResponse orderResp = str.DeJson<IfdianOrderResponse>();
                    return (orderResp.Data, orderResp.Ec == 200 ? string.Empty : orderResp.Em);
                }
                return (null, $"Ifdian api http error {resp.StatusCode}");
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                return (null, ex.Message);
            }
        }

        public sealed class IfdianOrderResponse
        {
            public int Ec { get; set; }
            public string Em { get; set; }
            public IfdianOrderData Data { get; set; }
        }

        public sealed class IfdianOrderData
        {
            public int Total_count { get; set; }
            public int Total_page { get; set; }
            public IfdianOrderItem[] List { get; set; } = Array.Empty<IfdianOrderItem>();
        }
        public sealed class IfdianOrderItem
        {
            public string Out_trade_no { get; set; }
            public string User_id { get; set; }
            public int Month { get; set; }
            public string Remark { get; set; }
            public string Plan_title { get; set; }
            public string User_name { get; set; }


        }

    }
}
