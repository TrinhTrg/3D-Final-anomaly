using System;
using UnityEngine;

public class AAH_RoomRegistry : MonoBehaviour
{
    [Serializable]
    public class RoomSlot
    {
        public int roomNumber;
        public Transform entry;
        public Transform bedApproach;
        public Transform bedPose;
        public Renderer bedRenderer;
        public AAH_PatientDoorAccess door;
        [NonSerialized] public string owner;
        [NonSerialized] public bool occupied;
    }
    public const int TreatmentRoomCount = 5;
    [SerializeField] private RoomSlot[] _rooms = new RoomSlot[TreatmentRoomCount];
    private static bool IsTreatmentRoom(RoomSlot room) =>
        room != null && room.roomNumber >= 1 && room.roomNumber <= TreatmentRoomCount;

    public RoomSlot[] Rooms => _rooms;
    public int FreeCount { get { int count = 0; foreach (var r in _rooms) if (IsTreatmentRoom(r) && string.IsNullOrEmpty(r.owner)) count++; return count; } }
    public event Action RoomsChanged;
    public RoomSlot Reserve(string patientId)
    {
        if (string.IsNullOrEmpty(patientId)) return null;
        foreach (var r in _rooms) if (IsTreatmentRoom(r) && r.owner == patientId) return r;
        foreach (var r in _rooms)
        {
            if (!IsTreatmentRoom(r) || !string.IsNullOrEmpty(r.owner) || r.entry == null || r.bedApproach == null || r.bedPose == null) continue;
            r.owner = patientId; r.occupied = false; RoomsChanged?.Invoke(); return r;
        }
        return null;
    }
    public bool Occupy(string patientId, int roomNumber)
    {
        foreach (var r in _rooms) if (IsTreatmentRoom(r) && r.roomNumber == roomNumber && r.owner == patientId)
        { r.occupied = true; RoomsChanged?.Invoke(); return true; }
        return false;
    }
    public bool ReleaseRoom(string patientId)
    {
        foreach (var r in _rooms) if (r != null && r.owner == patientId)
        { r.owner = null; r.occupied = false; RoomsChanged?.Invoke(); return true; }
        return false;
    }
    public bool ValidateConfiguration(out string error)
    {
        var numbers = new System.Collections.Generic.HashSet<int>();
        if (_rooms != null)
        {
            foreach (var room in _rooms)
            {
                // Older scenes also serialize a route for Room 6, which has no treatment system.
                if (!IsTreatmentRoom(room)) continue;
                if (!numbers.Add(room.roomNumber) || room.entry == null || room.bedApproach == null ||
                    room.bedPose == null || room.bedRenderer == null || room.door == null)
                { error = "Phòng điều trị bị trùng hoặc thiếu tham chiếu: " + room.roomNumber; return false; }
            }
        }
        if (numbers.Count != TreatmentRoomCount)
        { error = "Thiếu cấu hình phòng điều trị trong các phòng 1–5."; return false; }
        error = null; return true;
    }
}
