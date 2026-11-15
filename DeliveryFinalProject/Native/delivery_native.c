#include <math.h>
#include "delivery_native.h"

/* Returns rounded distance points plus a fixed Express bonus. */
DELIVERY_API int calculate_delivery_score(double distance_km, int is_express)
{
    if (!isfinite(distance_km) || distance_km < 0.0 || distance_km > 1000.0)
        return -1;

    if (is_express != 0 && is_express != 1)
        return -1;

    return (int)(distance_km * 10.0 + 0.5) + (is_express ? 50 : 0);
}
