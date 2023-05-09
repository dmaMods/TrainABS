using ColossalFramework;
using ColossalFramework.Globalization;
using UnityEngine;

namespace dmaTrainABS.Traffic
{
    public class Helpers
    {
        public static ushort ClearConfused()
        {
            ushort Confused = 0;
            string confusedStatus = Locale.Get("VEHICLE_STATUS_CONFUSED");
            PassengerCarAI passengerCarAI = Singleton<PassengerCarAI>.instance;
            VehicleManager vehicleManager = Singleton<VehicleManager>.instance;
            Vehicle[] vehicles = vehicleManager.m_vehicles.m_buffer;

            for (ushort v = 0; v < vehicles.Length; v++)
            {
                Vehicle vehicle = vehicles[v];
                if (vehicle.m_flags.IsFlagSet(Vehicle.Flags.Created) && vehicle.m_flags.IsFlagSet(Vehicle.Flags.Spawned) && vehicle.Info != null)
                {
                    if (vehicle.Info.m_vehicleType == VehicleInfo.VehicleType.Car && vehicle.Info.vehicleCategory == VehicleInfo.VehicleCategory.PassengerCar)
                    {
                        string vehicleStatus = passengerCarAI.GetLocalizedStatus(v, ref vehicle, out InstanceID instanceID);
                        if (vehicleStatus == confusedStatus && instanceID.IsEmpty)
                        {
                            vehicleManager.ReleaseVehicle(v);
                            Confused++;
                        }
                    }
                }
            }
#if DEBUG
            if (Confused != 0)
                Debug.Log("[TrainABS] Clear " + Confused + " confused cars.");
#endif
            return Confused;
        }

    }
}
