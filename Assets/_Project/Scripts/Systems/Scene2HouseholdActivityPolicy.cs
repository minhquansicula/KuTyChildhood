/// <summary>Four physical mini-games; other chores keep their story IDs, rewards and explicit UI confirmation.</summary>
public static class Scene2HouseholdActivityPolicy
{
    public static bool IsMinigame(HouseholdChore chore) =>
        chore == HouseholdChore.LightFire || chore == HouseholdChore.CookRice ||
        chore == HouseholdChore.WashDishes || chore == HouseholdChore.SetTable;

    public static bool IsActive(HouseholdChore chore) => IsMinigame(chore) || chore == HouseholdChore.ReadLetter;
    public static bool IsSimple(HouseholdChore chore) => chore == HouseholdChore.ReadLetter;

    public static string Description(HouseholdChore chore)
    {
        switch (chore)
        {
            case HouseholdChore.ArrangeKeepsakes: return "Xếp lại áo, khăn và kỷ vật trong rương. Những món nhỏ vẫn ở đây, như ngày con còn bé.";
            case HouseholdChore.TuneRadio: return "Bật radio và chỉnh về chương trình buổi sáng quen thuộc của nhà mình.";
            case HouseholdChore.CarryWood: return "Gom bó củi trong giỏ và mang đến cạnh bếp cho mẹ. Việc đặt từng thanh vào bếp sẽ làm ở bước nhóm lửa.";
            case HouseholdChore.FetchWater: return "Múc nước sạch từ lu để chuẩn bị bữa cơm. Mẹ đã để dành nước mát từ sáng.";
            case HouseholdChore.MeasureRice: return "Đong phần gạo vừa đủ cho hai mẹ con, chuẩn bị đem vo.";
            case HouseholdChore.WashRice: return "Vo gạo nhẹ tay, chắt nước đục và để gạo ráo trước khi nhóm bếp.";
            case HouseholdChore.PutAwayDishes: return "Cất chén và dĩa sạch theo từng loại vào tủ; đặt đũa gọn bên cạnh. Xong rồi, nghỉ tay một chút nhé.";
            case HouseholdChore.FoldBlanket: return "Gấp gọn chiếc chăn và đặt lại ở cuối giường.";
            case HouseholdChore.SortTools: return "Cất từng dụng cụ về đúng chỗ sau khi dùng. Đây là việc làm thêm, không bắt buộc.";
            case HouseholdChore.GrindCorn: return "Giúp mẹ chuẩn bị phần ngô đã xay. Đây là việc làm thêm, không bắt buộc.";
            case HouseholdChore.ReadLetter: return "“" + HouseholdChoreFlow.Memories[(int)HouseholdChore.ReadLetter] + "”\n\nĐọc thong thả lời mẹ để lại, rồi chọn Đọc xong khi con sẵn sàng.";
            default: return "Giúp mẹ hoàn tất việc nhỏ này.";
        }
    }
    public static string ConfirmLabel(HouseholdChore chore) => chore == HouseholdChore.ReadLetter ? "ĐỌC XONG" : "THỰC HIỆN";
}
