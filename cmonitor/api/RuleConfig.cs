using cmonitor.client.reports.snatch;
using common.libs.database;
using System.ComponentModel.DataAnnotations.Schema;

namespace cmonitor.api
{

    [Table("rule")]
    public sealed class RuleConfig
    {
        private readonly IConfigDataProvider<RuleConfig> configDataProvider;
        public RuleConfig() { }
        public RuleConfig(IConfigDataProvider<RuleConfig> configDataProvider)
        {
            this.configDataProvider = configDataProvider;
            RuleConfig config = configDataProvider.Load().Result ?? new RuleConfig
            {
                UserNames = new Dictionary<string, UserNameInfo> { { "snltty", new UserNameInfo {
                     Rules = new List<RulesInfo>{ new RulesInfo { ID = 1, Name = "默认" } },
                     Processs = new  List<GroupInfo>{ new GroupInfo { ID = 2, Name = "默认" } },
                      Windows = new List<WindowGroupInfo> { new WindowGroupInfo { ID = 3, Name = "默认" } },
                       Snatchs = new List<SnatchGroupInfo>{ new SnatchGroupInfo { ID=4, Name="默认" } }
                } } }
            };
            UserNames = config.UserNames;
            MaxID = config.MaxID;
            Save();
        }

        public Dictionary<string, UserNameInfo> UserNames { get; set; } = new Dictionary<string, UserNameInfo>();
        private uint maxid = 100;
        public uint MaxID
        {
            get => maxid; set
            {
                maxid = value;
            }
        }

        private readonly object lockObj = new object();

        public string AddName(string name)
        {
            lock (lockObj)
            {
                if (UserNames.ContainsKey(name) == false)
                {
                    UserNames.Add(name, new UserNameInfo
                    {
                        Rules = new List<RulesInfo> { new RulesInfo { ID = 1, Name = "默认" } },
                        Processs = new List<GroupInfo> { new GroupInfo { ID = 1, Name = "默认" } },
                        Windows = new List<WindowGroupInfo> { new WindowGroupInfo { ID = 1, Name = "默认" } },
                    });
                }
                Save();
            }

            return string.Empty;
        }

        public string AddProcessGroup(UpdateGroupInfo updateGroupInfo)
        {
            lock (lockObj)
            {
                if (UserNames.TryGetValue(updateGroupInfo.UserName, out UserNameInfo userNameInfo) == false)
                {
                    return "不存在此管理用户";
                }
                if (userNameInfo.Processs.FirstOrDefault(c => c.Name == updateGroupInfo.Group.Name && c.ID != updateGroupInfo.Group.ID) != null)
                {
                    return "已存在同名记录";
                }

                //添加
                if (updateGroupInfo.Group.ID == 0)
                {
                    updateGroupInfo.Group.ID = Interlocked.Increment(ref maxid);
                    userNameInfo.Processs.Add(updateGroupInfo.Group);
                    Save();
                    return string.Empty;
                }

                //修改
                GroupInfo old = userNameInfo.Processs.FirstOrDefault(c => c.ID == updateGroupInfo.Group.ID);
                if (old == null)
                {
                    return "不存在记录，无法修改";
                }
                old.Name = updateGroupInfo.Group.Name;
                Save();
            }

            return string.Empty;
        }
        public string AddProcess(UpdateItemInfo updateItem)
        {
            lock (lockObj)
            {
                if (UserNames.TryGetValue(updateItem.UserName, out UserNameInfo userNameInfo) == false)
                {
                    return "不存在此管理用户";
                }
                GroupInfo group = userNameInfo.Processs.FirstOrDefault(c => c.ID == updateItem.GroupID);
                if (group == null)
                {
                    return "不存在此分组";
                }
                if (group.List.FirstOrDefault(c => c.Name == updateItem.Item.Name && c.ID != updateItem.Item.ID) != null)
                {
                    return "已存在同名记录";
                }

                //添加
                if (updateItem.Item.ID == 0)
                {
                    updateItem.Item.ID = Interlocked.Increment(ref maxid);
                    group.List.Add(updateItem.Item);
                    Save();
                    return string.Empty;
                }

                //修改
                ItemInfo old = group.List.FirstOrDefault(c => c.ID == updateItem.Item.ID);
                if (old == null)
                {
                    return "不存在记录，无法修改";
                }
                old.Name = updateItem.Item.Name;
                old.AllowType = updateItem.Item.AllowType;
                old.DataType = updateItem.Item.DataType;
                Save();
            }
            return string.Empty;
        }
        public string DeleteProcessGroup(DeleteGroupInfo deleteGroupInfo)
        {
            lock (lockObj)
            {
                if (UserNames.TryGetValue(deleteGroupInfo.UserName, out UserNameInfo userNameInfo) == false)
                {
                    return "不存在此管理用户";
                }

                userNameInfo.Processs.Remove(userNameInfo.Processs.FirstOrDefault(c => c.ID == deleteGroupInfo.ID));

                Save();
            }

            return string.Empty;
        }
        public string DeleteProcess(DeleteItemInfo deleteItemInfo)
        {
            lock (lockObj)
            {
                if (UserNames.TryGetValue(deleteItemInfo.UserName, out UserNameInfo userNameInfo) == false)
                {
                    return "不存在此管理用户";
                }
                GroupInfo group = userNameInfo.Processs.FirstOrDefault(c => c.ID == deleteItemInfo.GroupID);
                if (group == null)
                {
                    return "不存在此分组";
                }

                group.List.Remove(group.List.FirstOrDefault(c => c.ID == deleteItemInfo.ID));
            }
            return string.Empty;
        }

        public string AddRule(UpdateRuleInfo updateRuleInfo)
        {
            lock (lockObj)
            {
                if (UserNames.TryGetValue(updateRuleInfo.UserName, out UserNameInfo userNameInfo) == false)
                {
                    return "不存在此管理用户";
                }
                if (userNameInfo.Rules.FirstOrDefault(c => c.Name == updateRuleInfo.Rule.Name && c.ID != updateRuleInfo.Rule.ID) != null)
                {
                    return "已存在同名记录";
                }

                //添加
                if (updateRuleInfo.Rule.ID == 0)
                {
                    updateRuleInfo.Rule.ID = Interlocked.Increment(ref maxid);
                    userNameInfo.Rules.Add(updateRuleInfo.Rule);
                    Save();
                    return string.Empty;
                }

                //修改
                RulesInfo old = userNameInfo.Rules.FirstOrDefault(c => c.ID == updateRuleInfo.Rule.ID);
                if (old == null)
                {
                    return "不存在记录，无法修改";
                }
                old.Name = updateRuleInfo.Rule.Name;
                old.PrivateProcesss = updateRuleInfo.Rule.PrivateProcesss;
                old.PublicProcesss = updateRuleInfo.Rule.PublicProcesss;
                Save();
            }

            return string.Empty;
        }
        public string DeleteRule(DeleteRuleInfo deleteRuleInfo)
        {
            lock (lockObj)
            {
                if (UserNames.TryGetValue(deleteRuleInfo.UserName, out UserNameInfo userNameInfo) == false)
                {
                    return "不存在此管理用户";
                }

                userNameInfo.Rules.Remove(userNameInfo.Rules.FirstOrDefault(c => c.ID == deleteRuleInfo.ID));
                Save();
            }

            return string.Empty;
        }

        public string UpdateDevices(UpdateDevicesInfo updatDevicesInfo)
        {
            lock (lockObj)
            {
                if (UserNames.TryGetValue(updatDevicesInfo.UserName, out UserNameInfo userNameInfo) == false)
                {
                    return "不存在此管理用户";
                }

                userNameInfo.Devices = updatDevicesInfo.Devices;
                Save();
            }
            return string.Empty;
        }

        public string AddWindowGroup(UpdateWindowGroupInfo updateGroupInfo)
        {
            lock (lockObj)
            {
                if (UserNames.TryGetValue(updateGroupInfo.UserName, out UserNameInfo userNameInfo) == false)
                {
                    return "不存在此管理用户";
                }
                if (userNameInfo.Windows.FirstOrDefault(c => c.Name == updateGroupInfo.Group.Name && c.ID != updateGroupInfo.Group.ID) != null)
                {
                    return "已存在同名记录";
                }

                //添加
                if (updateGroupInfo.Group.ID == 0)
                {
                    updateGroupInfo.Group.ID = Interlocked.Increment(ref maxid);
                    userNameInfo.Windows.Add(updateGroupInfo.Group);
                    Save();
                    return string.Empty;
                }

                //修改
                WindowGroupInfo old = userNameInfo.Windows.FirstOrDefault(c => c.ID == updateGroupInfo.Group.ID);
                if (old == null)
                {
                    return "不存在记录，无法修改";
                }
                old.Name = updateGroupInfo.Group.Name;
                Save();
            }

            return string.Empty;
        }
        public string DeleteWindowGroup(DeleteWindowGroupInfo deleteGroupInfo)
        {
            lock (lockObj)
            {
                if (UserNames.TryGetValue(deleteGroupInfo.UserName, out UserNameInfo userNameInfo) == false)
                {
                    return "不存在此管理用户";
                }

                userNameInfo.Windows.Remove(userNameInfo.Windows.FirstOrDefault(c => c.ID == deleteGroupInfo.ID));

                Save();
            }

            return string.Empty;
        }
        public string AddWindow(AddWindowItemInfo updateItem)
        {
            lock (lockObj)
            {
                if (UserNames.TryGetValue(updateItem.UserName, out UserNameInfo userNameInfo) == false)
                {
                    return "不存在此管理用户";
                }
                WindowGroupInfo group = userNameInfo.Windows.FirstOrDefault(c => c.ID == updateItem.GroupID);
                if (group == null)
                {
                    return "不存在此分组";
                }
                if (group.List.FirstOrDefault(c => c.Name == updateItem.Item.Name && c.ID != updateItem.Item.ID) != null)
                {
                    return "已存在同名记录";
                }

                //添加
                if (updateItem.Item.ID == 0)
                {
                    updateItem.Item.ID = Interlocked.Increment(ref maxid);
                    group.List.Add(updateItem.Item);
                    Save();
                    return string.Empty;
                }

                //修改
                WindowItemInfo old = group.List.FirstOrDefault(c => c.ID == updateItem.Item.ID);
                if (old == null)
                {
                    return "不存在记录，无法修改";
                }
                old.Name = updateItem.Item.Name;
                old.Desc = updateItem.Item.Desc;
                Save();
            }
            return string.Empty;
        }
        public string DelWindow(DeletedWindowItemInfo deletedFileNameInfo)
        {
            lock (lockObj)
            {
                if (UserNames.TryGetValue(deletedFileNameInfo.UserName, out UserNameInfo userNameInfo) == false)
                {
                    return "不存在此管理用户";
                }
                WindowGroupInfo group = userNameInfo.Windows.FirstOrDefault(c => c.ID == deletedFileNameInfo.GroupID);
                if (group == null)
                {
                    return "不存在此分组";
                }

                group.List.Remove(group.List.FirstOrDefault(c => c.ID == deletedFileNameInfo.ID));
                Save();
            }
            return string.Empty;
        }


        public string AddSnatchGroup(UpdateSnatchGroupInfo updateGroupInfo)
        {
            lock (lockObj)
            {
                if (UserNames.TryGetValue(updateGroupInfo.UserName, out UserNameInfo userNameInfo) == false)
                {
                    return "不存在此管理用户";
                }
                if (userNameInfo.Windows.FirstOrDefault(c => c.Name == updateGroupInfo.Group.Name && c.ID != updateGroupInfo.Group.ID) != null)
                {
                    return "已存在同名记录";
                }

                //添加
                if (updateGroupInfo.Group.ID == 0)
                {
                    updateGroupInfo.Group.ID = Interlocked.Increment(ref maxid);
                    userNameInfo.Snatchs.Add(updateGroupInfo.Group);
                    Save();
                    return string.Empty;
                }

                //修改
                SnatchGroupInfo old = userNameInfo.Snatchs.FirstOrDefault(c => c.ID == updateGroupInfo.Group.ID);
                if (old == null)
                {
                    return "不存在记录，无法修改";
                }
                old.Name = updateGroupInfo.Group.Name;
                Save();
            }

            return string.Empty;
        }
        public string DeleteSnatchGroup(DeleteSnatchGroupInfo deleteGroupInfo)
        {
            lock (lockObj)
            {
                if (UserNames.TryGetValue(deleteGroupInfo.UserName, out UserNameInfo userNameInfo) == false)
                {
                    return "不存在此管理用户";
                }

                userNameInfo.Snatchs.Remove(userNameInfo.Snatchs.FirstOrDefault(c => c.ID == deleteGroupInfo.ID));

                Save();
            }

            return string.Empty;
        }
        public string AddSnatch(AddSnatchItemInfo updateItem)
        {
            lock (lockObj)
            {
                if (UserNames.TryGetValue(updateItem.UserName, out UserNameInfo userNameInfo) == false)
                {
                    return "不存在此管理用户";
                }
                SnatchGroupInfo group = userNameInfo.Snatchs.FirstOrDefault(c => c.ID == updateItem.GroupID);
                if (group == null)
                {
                    return "不存在此分组";
                }
                if (group.List.FirstOrDefault(c => c.Title == updateItem.Item.Title && c.ID != updateItem.Item.ID) != null)
                {
                    return "已存在同名记录";
                }

                //添加
                if (updateItem.Item.ID == 0)
                {
                    updateItem.Item.ID = Interlocked.Increment(ref maxid);
                    group.List.Add(updateItem.Item);
                    Save();
                    return string.Empty;
                }

                //修改
                SnatchItemInfo old = group.List.FirstOrDefault(c => c.ID == updateItem.Item.ID);
                if (old == null)
                {
                    return "不存在记录，无法修改";
                }
                old.Type = updateItem.Item.Type;
                old.Title = updateItem.Item.Title;
                old.Question = updateItem.Item.Question;
                old.Options = updateItem.Item.Options;
                old.Correct = updateItem.Item.Correct;
                old.Chance = updateItem.Item.Chance;

                Save();
            }
            return string.Empty;
        }
        public string DelSnatch(DeletedSnatchItemInfo deletedFileNameInfo)
        {
            lock (lockObj)
            {
                if (UserNames.TryGetValue(deletedFileNameInfo.UserName, out UserNameInfo userNameInfo) == false)
                {
                    return "不存在此管理用户";
                }
                SnatchGroupInfo group = userNameInfo.Snatchs.FirstOrDefault(c => c.ID == deletedFileNameInfo.GroupID);
                if (group == null)
                {
                    return "不存在此分组";
                }

                group.List.Remove(group.List.FirstOrDefault(c => c.ID == deletedFileNameInfo.ID));
                Save();
            }
            return string.Empty;
        }

        public SnatchItemInfo[] SnatchRandom(int length)
        {
            return UserNames.Values
                .SelectMany(c => c.Snatchs)
                .SelectMany(c => c.List).OrderBy(c => Guid.NewGuid())
                .Where(c => c.Cate == SnatchCate.Question)
                .Where(c => (c.Type == SnatchType.Select && c.Options.Any(c => c.Value)) || (c.Type == SnatchType.Input && string.IsNullOrWhiteSpace(c.Correct) == false))
                .Take(length).ToArray();
        }

        public void Save()
        {
            configDataProvider.Save(this).Wait();
        }
    }

    public sealed class UserNameInfo
    {
        public List<RulesInfo> Rules { get; set; } = new List<RulesInfo>();
        public List<GroupInfo> Processs { get; set; } = new List<GroupInfo>();
        public List<string> Devices { get; set; } = new List<string>();
        public List<WindowGroupInfo> Windows { get; set; } = new List<WindowGroupInfo>();
        public List<SnatchGroupInfo> Snatchs { get; set; } = new List<SnatchGroupInfo>();
    }
    public sealed class UpdateDevicesInfo
    {
        public string UserName { get; set; }
        public List<string> Devices { get; set; } = new List<string>();
    }


    public sealed class RulesInfo
    {
        public uint ID { get; set; }
        public string Name { get; set; }
        public List<uint> PrivateProcesss { get; set; } = new List<uint>();
        public List<uint> PublicProcesss { get; set; } = new List<uint>();
    }
    public sealed class UpdateRuleInfo
    {
        public string UserName { get; set; }
        public RulesInfo Rule { get; set; }
    }
    public sealed class DeleteRuleInfo
    {
        public string UserName { get; set; }
        public uint ID { get; set; }
    }

    public sealed class GroupInfo
    {
        public uint ID { get; set; }
        public string Name { get; set; }
        public List<ItemInfo> List { get; set; } = new List<ItemInfo>();
    }
    public sealed class UpdateGroupInfo
    {
        public string UserName { get; set; }
        public GroupInfo Group { get; set; }
    }
    public sealed class DeleteGroupInfo
    {
        public string UserName { get; set; }
        public uint ID { get; set; }
    }

    public sealed class ItemInfo
    {
        public uint ID { get; set; }
        public string Name { get; set; }
        public DataType DataType { get; set; }
        public AllowType AllowType { get; set; }
    }
    public enum DataType
    {
        Process = 0,
        Domain = 1,
        IP = 2,
    }
    public enum AllowType
    {
        Allow = 0,
        Denied = 1
    }

    public sealed class UpdateItemInfo
    {
        public string UserName { get; set; }
        public uint GroupID { get; set; }
        public ItemInfo Item { get; set; }
    }
    public sealed class DeleteItemInfo
    {
        public string UserName { get; set; }
        public uint GroupID { get; set; }
        public uint ID { get; set; }
    }



    public sealed class WindowGroupInfo
    {
        public uint ID { get; set; }
        public string Name { get; set; }
        public List<WindowItemInfo> List { get; set; } = new List<WindowItemInfo>();
    }
    public sealed class UpdateWindowGroupInfo
    {
        public string UserName { get; set; }
        public WindowGroupInfo Group { get; set; }
    }
    public sealed class DeleteWindowGroupInfo
    {
        public string UserName { get; set; }
        public uint ID { get; set; }
    }
    public sealed class WindowItemInfo
    {
        public uint ID { get; set; }
        public string Name { get; set; }
        public string Desc { get; set; }
    }
    public sealed class AddWindowItemInfo
    {
        public string UserName { get; set; }
        public uint GroupID { get; set; }
        public WindowItemInfo Item { get; set; }
    }
    public sealed class DeletedWindowItemInfo
    {
        public string UserName { get; set; }
        public uint GroupID { get; set; }
        public uint ID { get; set; }
    }



    public sealed class SnatchGroupInfo
    {
        public uint ID { get; set; }
        public string Name { get; set; }
        public List<SnatchItemInfo> List { get; set; } = new List<SnatchItemInfo>();
    }
    public sealed class UpdateSnatchGroupInfo
    {
        public string UserName { get; set; }
        public SnatchGroupInfo Group { get; set; }
    }
    public sealed class DeleteSnatchGroupInfo
    {
        public string UserName { get; set; }
        public uint ID { get; set; }
    }
    public sealed class AddSnatchItemInfo
    {
        public string UserName { get; set; }
        public uint GroupID { get; set; }
        public SnatchItemInfo Item { get; set; }
    }
    public sealed class DeletedSnatchItemInfo
    {
        public string UserName { get; set; }
        public uint GroupID { get; set; }
        public uint ID { get; set; }
    }


    public sealed class SnatchItemInfo
    {
        public uint ID { get; set; }
        public string Title { get; set; }

        public SnatchCate Cate { get; set; } = SnatchCate.Question;
        public SnatchType Type { get; set; } = SnatchType.Select;
        /// <summary>
        /// 问题
        /// </summary>
        public string Question { get; set; }
        /// <summary>
        /// 选项数
        /// </summary>
        public List<SnatchItemOptionInfo> Options { get; set; }
        /// <summary>
        /// 答案
        /// </summary>
        public string Correct { get; set; }
        /// <summary>
        /// 最多答题次数
        /// </summary>
        public int Chance { get; set; }
    }
    public sealed class SnatchItemOptionInfo
    {
        public string Text { get; set; }
        public bool Value { get; set; }
    }
}
