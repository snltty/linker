export default {
    field() {
        return {
            ShareSnatch: {
                Question: { Index: 0, Value: { Type: 1, Question: '', Correct: '', Option: 0, Max: 0, End: false, Repeat: false, Join: 0, Right: 0, Wrong: 0 } },
                Answer: { Index: 0 },
            }
        };
    },
    state: {
        shareSnatch: {
            showTemplate: false,
            showUse: true
        }
    },
    init() {
    },
    update(item, report) {
        if (!report.Share) return;

        console.log(report.Share);

        if (report.Share.SnatchQuestion) {
            item.ShareSnatch.Question.Index = report.Share.SnatchQuestion.Index;
            const json = JSON.parse(report.Share.SnatchQuestion.Value);
            item.ShareSnatch.Question.Value.Type = json.Type;
            item.ShareSnatch.Question.Value.Question = json.Question;
            item.ShareSnatch.Question.Value.Correct = json.Correct;
            item.ShareSnatch.Question.Value.Option = json.Option;
            item.ShareSnatch.Question.Value.Max = json.Max;
            item.ShareSnatch.Question.Value.End = json.End;
            item.ShareSnatch.Question.Value.Repeat = json.Repeat;
            item.ShareSnatch.Question.Value.Join = json.Join;
            item.ShareSnatch.Question.Value.Right = json.Right;
            item.ShareSnatch.Question.Value.Wrong = json.Wrong;
        }
        if (report.Share.SnatchAnswer) {
            item.ShareSnatch.Answer.Index = report.Share.SnatchAnswer.Index;
        }
    }
}