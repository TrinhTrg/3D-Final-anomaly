using UnityEngine;

[CreateAssetMenu(menuName = "AAH/Day 1 Dialogue")]
public class AAH_Day1Dialogue : ScriptableObject
{
    [System.Serializable]
    public class Line
    {
        public string key;
        [TextArea] public string vietnamese;
        public Line(string key, string vietnamese) { this.key = key; this.vietnamese = vietnamese; }
    }
    [SerializeField] private Line[] _arrival = {
        new Line("day1.polly.arrival.1", "Chào bác sĩ... Tôi bị đau đầu mấy hôm nay rồi."),
        new Line("day1.polly.arrival.2", "Công việc nhiều quá, ngày nào tôi cũng phải tăng ca."),
        new Line("day1.polly.arrival.3", "Tối qua tôi gần như không ngủ được. Đầu cứ đau liên tục...")
    };
    [SerializeField] private Line[] _examination = {
        new Line("day1.polly.examination.1", "Bác sĩ kiểm tra giúp tôi với. Tôi mệt quá rồi."),
        new Line("day1.polly.examination.2", "Không biết có nghiêm trọng không nữa...")
    };
    [SerializeField] private Line[] _recovery = {
        new Line("day1.polly.recovery.1", "Ôi, dễ chịu hơn nhiều rồi! Cảm ơn bác sĩ nhé!"),
        new Line("day1.polly.recovery.2", "Chắc tôi phải xin nghỉ phép vài hôm thôi.")
    };
    [SerializeField] private Line[] _mupple = {
        new Line("day1.muple.welcome", "À, cậu là nhân viên mới được tuyển dụng đúng không? Chào mừng đến với bệnh viện!"),
        new Line("day1.muple.first_day", "Ngày đầu làm việc thế nào? Chắc cũng không quá khó nhỉ?"),
        new Line("day1.muple.one_more_thing", "À, còn một điều nữa..."),
        new Line("day1.muple.warning", "Nếu mọi thứ trở nên kỳ lạ, hãy cứ tiếp tục làm việc."),
        new Line("day1.muple.shutter", "Hãy nhấn vào nút Shutter để từ chối vị khách bất thường"),
        new Line("day1.muple.goodbye", "Thôi, chúc cậu một ca trực tốt lành!")
    };
    public Line[] Arrival => _arrival;
    public Line[] Examination => _examination;
    public Line[] Recovery => _recovery;
    public Line[] Mupple => _mupple;
    // A localization adapter can resolve each stable key; Vietnamese remains the fallback.
    public System.Func<string, string> ResolveLocalizedKey { get; set; }
    public string Resolve(Line line)
    {
        string localized = ResolveLocalizedKey?.Invoke(line.key);
        return string.IsNullOrEmpty(localized) ? line.vietnamese : localized;
    }
}
