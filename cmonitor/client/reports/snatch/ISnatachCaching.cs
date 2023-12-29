using common.libs.extends;
using System.Collections.Concurrent;

namespace cmonitor.client.reports.snatch
{
    public interface ISnatachCaching
    {
        /// <summary>
        /// 添加一个互动
        /// </summary>
        /// <param name="info"></param>
        /// <param name="q">可以为null，留作后续操作</param>
        /// <returns></returns>
        public bool Add(SnatchQuestionCacheInfo info, SnatchQuestionInfo q);
        /// <summary>
        /// 更新一个设备的题目
        /// </summary>
        /// <param name="username"></param>
        /// <param name="machineName"></param>
        /// <param name="quesion"></param>
        /// <returns></returns>
        public bool Update(string username, string machineName, SnatchQuestionInfo quesion);
        /// <summary>
        /// 更新设备的回答
        /// </summary>
        /// <param name="machineName"></param>
        /// <param name="answer"></param>
        /// <returns></returns>
        public bool Update(string machineName, SnatchAnswerInfo answer);
        /// <summary>
        /// 移除一个互动
        /// </summary>
        /// <param name="username"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool Remove(string username, out SnatchQuestionCacheInfo info);

        /// <summary>
        /// 获取一个互动
        /// </summary>
        /// <param name="username"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool Get(string username, out SnatchQuestionCacheInfo info);
        /// <summary>
        /// 获取设备的回答
        /// </summary>
        /// <param name="username"></param>
        /// <param name="machineName"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool Get(string username, string machineName, out SnatchAnswerInfo answer);
        /// <summary>
        /// 获取相同题目的所有设备回答，先获取设备回答，再拿回答来这里获取
        /// </summary>
        /// <param name="answer"></param>
        /// <returns></returns>
        public SnatchAnswerInfo[] Get(SnatchAnswerInfo answer);
    }

    public sealed class SnatchQuestionCacheInfo
    {
        public string UserName { get; set; }
        public List<SnatchAnswerInfo> Answers { get; set; }
        public string[] MachineNames { get; set; }
    }

    public sealed class SnatachCachingMemory : ISnatachCaching
    {
        private readonly ConcurrentDictionary<string, SnatchQuestionCacheInfo> cache = new ConcurrentDictionary<string, SnatchQuestionCacheInfo>();
        public SnatachCachingMemory()
        {
        }

        public bool Add(SnatchQuestionCacheInfo info, SnatchQuestionInfo quesion)
        {
            if (cache.TryGetValue(info.UserName, out SnatchQuestionCacheInfo _info) == false)
            {
                info.Answers = info.MachineNames.Select(c => new SnatchAnswerInfo { UserName = info.UserName, Question = quesion, Times = 0, MachineName = c, Result = false, ResultStr = string.Empty, State = SnatchState.Ask, Time = 0 }).ToList();
                
                cache.TryAdd(info.UserName, info);
                foreach (SnatchAnswerInfo item in info.Answers)
                {
                    UpdateRightWrong(item);
                }
                return true;
            }
            return false;
        }
        public bool Update(string username, string machineName, SnatchQuestionInfo quesion)
        {
            if (cache.TryGetValue(username, out SnatchQuestionCacheInfo _info))
            {
                SnatchAnswerInfo answer = _info.Answers.FirstOrDefault(c => c.MachineName == machineName);
                if (answer != null)
                {
                    answer.Question = quesion;
                    UpdateRightWrong(answer);
                    return true;
                }
            }
            return false;
        }

        public bool Update(string machineName, SnatchAnswerInfo answer)
        {
            if (cache.TryGetValue(answer.UserName, out SnatchQuestionCacheInfo info))
            {
                SnatchAnswerInfo _answer = info.Answers.FirstOrDefault(c => c.MachineName == machineName);
                if (_answer == null)
                {
                    return false;
                }
                _answer.State = answer.State;
                _answer.Time = answer.Time;
                _answer.ResultStr = answer.ResultStr;
                _answer.Result = answer.ResultStr == _answer.Question.Correct;

                UpdateRightWrong(_answer);
            }
            return true;
        }
        private void UpdateRightWrong(SnatchAnswerInfo answer)
        {
            if (cache.TryGetValue(answer.UserName, out SnatchQuestionCacheInfo info) && answer.Question != null)
            {
                IEnumerable<SnatchAnswerInfo> answers = info.Answers.Where(c => ReferenceEquals(c.Question, answer.Question));
                int join = answers.Count();
                int right = 0;
                int wrong = 0;
                if (answer.Question.Cate == SnatchCate.Question)
                {
                    right = answers.Count(c => c.Result);
                    wrong = answers.Count(c => string.IsNullOrWhiteSpace(c.ResultStr) == false && c.Result == false);
                }
                else
                {
                    right = answers.Count(c => string.IsNullOrWhiteSpace(c.ResultStr) == false);
                    wrong = answers.Count(c => string.IsNullOrWhiteSpace(c.ResultStr));
                }
                foreach (SnatchAnswerInfo item in answers)
                {
                    item.Question.Join = join;
                    item.Question.Right = right;
                    item.Question.Wrong = wrong;
                }
            }
        }


        public bool Remove(string username, out SnatchQuestionCacheInfo info)
        {
            if (cache.TryRemove(username, out info))
            {
                foreach (SnatchAnswerInfo item in info.Answers)
                {
                    item.Question.End = true;
                }
                return true;
            }
            return false;
        }

        public bool Get(string username, out SnatchQuestionCacheInfo info)
        {
            return cache.TryGetValue(username, out info);
        }
        public bool Get(string username, string machineName, out SnatchAnswerInfo info)
        {
            info = null;
            if (cache.TryGetValue(username, out SnatchQuestionCacheInfo cacheInfo))
            {
                info = cacheInfo.Answers.FirstOrDefault(c => c.MachineName == machineName);
            }
            return info != null;
        }
        public SnatchAnswerInfo[] Get(SnatchAnswerInfo answer)
        {
            if (cache.TryGetValue(answer.UserName, out SnatchQuestionCacheInfo info) && answer.Question != null)
            {
                return info.Answers.Where(c => ReferenceEquals(c.Question, answer.Question)).ToArray();
            }
            return Array.Empty<SnatchAnswerInfo>();
        }
    }
}
