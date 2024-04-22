namespace AlfaMicroserviceMesh.Helpers;

public class Helper {
    public static string GetNextCyclicItem(List<string> list, string item) {
        if (list.Count < 1) return null!;

        int index = list.IndexOf(item);
        int nextIndex = (index + 1) % list.Count;

        while (true) return list[nextIndex];
    }
}
