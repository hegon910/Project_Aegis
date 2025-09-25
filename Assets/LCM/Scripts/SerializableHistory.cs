using System.Collections.Generic;

[System.Serializable]
public class SerializableHistory
{
    public List<SimpleEventRecord> eventRecords;
    public List<GamePlaythroughRecord> playthroughRecords;

    // 생성자를 통해 두 리스트를 한 번에 담을 수 있습니다.
    public SerializableHistory(List<SimpleEventRecord> events, List<GamePlaythroughRecord> playthroughs)
    {
        eventRecords = events;
        playthroughRecords = playthroughs;
    }
}