using UnityEngine;

public static class ResponseCurves
{
    public static float Logistic(float x, float k, float m)
    {
        return 1f / (1f + Mathf.Exp(-k * (x - m)));
    }

    public static float Polynomial(float x, float exponent)
    {
        return Mathf.Pow(Mathf.Clamp01(x), exponent);
    }
}