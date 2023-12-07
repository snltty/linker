using System.Collections.Concurrent;

namespace cmonitor.client.reports.snatch
{
    public interface ISnatachCaching
    {
        public bool Add(SnatchQuestionCacheInfo info);
        public bool Update(string name, SnatchAnswerInfo answer);
        public bool Remove(string name, out SnatchQuestionCacheInfo info);

        public bool Get(string name, out SnatchQuestionCacheInfo info);
    }

    public sealed class SnatchQuestionCacheInfo
    {
        public string Name { get; set; }
        public SnatchQuestionInfo Question { get; set; }
        public List<SnatchAnswerInfo> Answers { get; set; }
        public string[] Names { get; set; }
    }

    public sealed class SnatachCachingMemory : ISnatachCaching
    {
        private readonly ConcurrentDictionary<string, SnatchQuestionCacheInfo> cache = new ConcurrentDictionary<string, SnatchQuestionCacheInfo>();
        public SnatachCachingMemory() { }

        public bool Add(SnatchQuestionCacheInfo info)
        {
            if (cache.TryGetValue(info.Name, out SnatchQuestionCacheInfo _info) == false)
            {
                info.Question.Name = info.Name;
                info.Answers = info.Names.Select(c => new SnatchAnswerInfo { Name = c, Result = false, ResultStr = string.Empty, State = SnatchState.Ask, Time = 0 }).ToList();
                info.Question.Join = info.Names.Length;
                cache.TryAdd(info.Name, info);
                return true;
            }
            return false;
        }
        public bool Update(string name, SnatchAnswerInfo answer)
        {
            if (cache.TryGetValue(answer.Name, out SnatchQuestionCacheInfo info))
            {
                SnatchAnswerInfo _answer = info.Answers.FirstOrDefault(c => c.Name == name);
                if (_answer != null)
                {
                    _answer.State = answer.State;
                    _answer.Result = answer.ResultStr == info.Question.Correct;
                    _answer.Time = answer.Time;
                    _answer.ResultStr = answer.ResultStr;
                }
                if(info.Question.Cate == SnatchCate.Question)
                {
                    info.Question.Right = info.Answers.Count(c => c.Result);
                    info.Question.Wrong = info.Answers.Count(c => string.IsNullOrWhiteSpace(c.ResultStr) == false && c.Result == false);
                }
                else
                {
                    info.Question.Right = info.Answers.Count(c => string.IsNullOrWhiteSpace(c.ResultStr) == false);
                    info.Question.Wrong = info.Answers.Count(c => string.IsNullOrWhiteSpace(c.ResultStr));
                }
            }
            return true;
        }

        public bool Remove(string name, out SnatchQuestionCacheInfo info)
        {
            if (cache.TryRemove(name, out info))
            {
                info.Question.End = true;
                return true;
            }
            return false;
        }

        public bool Get(string name, out SnatchQuestionCacheInfo info)
        {
            return cache.TryGetValue(name, out info);
        }
    }
}
