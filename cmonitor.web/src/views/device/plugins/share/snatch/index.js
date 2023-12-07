export default {
    field() {
        return {
        };
    },
    state: {
        shareSnatch: {
            showTemplate: false,
            showUse: false,
            question: {
                Question: { Name: '', Cate: 1, Type: 1, Question: '', Correct: '', Option: 0, Chance: 0, End: true, Join: 0, Right: 0, Wrong: 0 },
                Name: '',
                Names: [],
                Answers: [],
            }
        }
    },
    init() {
    },
    update(item, report) {
        if (!report.Share) return;
    }
}