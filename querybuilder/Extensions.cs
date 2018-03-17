using System;

namespace querybuilder
{
  public static class Extensions
  {
    public static bool EstaPreenchido(this Guid pValue)
    {
      if (pValue == Guid.Empty)
        return false;
      else
        return true;
    }

    public static bool EstaVazio(this Guid pValue)
    {
      if (pValue == Guid.Empty)
        return true;
      else
        return false;
    }

    public static Object GetParameterValue(this Object pValue)
    {
      if (pValue == null)
        return DBNull.Value;
      else if (pValue is Guid)
      {
        Guid key = (Guid)pValue;
        if (key.EstaPreenchido())
          return key;
        return DBNull.Value;
      }
      else if (pValue.GetType().IsEnum)
        return pValue.ToString();
      else if (pValue is Boolean)
      {
        Boolean bvle = (Boolean)pValue;
        return bvle ? true : false;
      }
      return pValue;
    }

  }
}
