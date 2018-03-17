using System;
using System.Collections.Generic;
using System.Linq;

namespace querybuilder
{
  public interface ISQLStatement
  {
  }

  public class SQLStringValue
  {
    public SQLStringValue(String pValue)
    {
      _Value = pValue;
    }

    public SQLStringValue(object pValue)
    {
      _Value = pValue.GetParameterValue().ToString();
    }

    public SQLStringValue(Guid pValue)
    {
      _Value = pValue.ToString();
    }

    public SQLStringValue(int pValue)
    {
      _Value = pValue.ToString();
    }

    private readonly string _Value;

    public override string ToString()
    {
      return String.Format("'{0}'", _Value);
    }
  }

  public class SQlDateTimeValue
  {
    public SQlDateTimeValue(DateTime pDateTime)
    {
      _DateTime = pDateTime;
    }

    private DateTime _DateTime;

    public override string ToString()
    {
      return String.Format("'{0}'", _DateTime.ToString("o"));
      //return String.Format("'{0}'", _DateTime.ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
    }
  }

  public class SQlBooleanValue
  {
    public SQlBooleanValue(Boolean pValue)
    {
      _Value = pValue;
    }

    private readonly Boolean _Value;

    public override string ToString()
    {
      return String.Format("'{0}'", _Value ? "Sim" : "Não");
    }
  }

  public class SQLParameter
  {
    private readonly string _Name;

    public SQLParameter(string pName)
    {
      _Name = pName;
    }

    public override string ToString()
    {
      return String.Format("@{0}", _Name);
    }

    public string Cast(string type)
    {
      return String.Format("CAST(@{0} AS {1})", _Name, type);
    }
  }

  public class SQLConvert
  {
    private readonly object _Value;

    public SQLConvert(object pValue)
    {
      _Value = pValue;
    }

    public override string ToString()
    {
      if (_Value is DateTime)
        return new SQlDateTimeValue((DateTime)_Value).ToString();
      if (_Value is bool)
        return new SQlBooleanValue((Boolean)_Value).ToString();
      return new SQLStringValue(_Value.ToString()).ToString();
    }
  }

  public enum SQLComparator
  {
    Equal,
    NotEqual,
    Like,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual
  }

  public interface IOperator : ISQLStatement
  {
  }

  public class And : IOperator
  {
    public override string ToString()
    {
      return "AND";
    }
  }

  public class Or : IOperator
  {
    public override string ToString()
    {
      return "OR";
    }
  }

  public class Null : IOperator
  {
    protected string _Field;

    public Null(string pField)
    {
      _Field = pField;
    }

    public override string ToString()
    {
      return String.Format("{0} IS NULL", _Field);
    }
  }

  public class NotNull : Null
  {
    public NotNull(string pField)
      : base(pField)
    {
    }

    public override string ToString()
    {
      return String.Format("{0} IS NOT NULL", _Field);
    }
  }

  public class Not : IOperator
  {
    public override string ToString()
    {
      return "NOT";
    }
  }

  public class Comparator : IOperator
  {
    public Comparator(string pFieldLeft, SQLComparator pComparator, string pFieldRigth)
    {
      _FieldLeft = pFieldLeft;
      _FieldRigth = pFieldRigth;
      _Comparator = pComparator;
    }

    public Comparator(IWhere pWhere)
    {
      _Where = pWhere;
    }

    private readonly String _FieldLeft;
    private readonly String _FieldRigth;
    private readonly SQLComparator _Comparator;
    private readonly IWhere _Where;

    private static string ConvertSQLComparator(SQLComparator pComparator)
    {
      switch (pComparator)
      {
        case SQLComparator.Equal:
          return "=";

        case SQLComparator.NotEqual:
          return "<>";

        case SQLComparator.Like:
          return "LIKE";

        case SQLComparator.GreaterThan:
          return ">";

        case SQLComparator.LessThan:
          return "<";

        case SQLComparator.GreaterThanOrEqual:
          return ">=";

        case SQLComparator.LessThanOrEqual:
          return "<=";

        default:
          throw new Exception("Invalid Comparator");
      }
    }

    public override string ToString()
    {
      if (_Where == null)
        return String.Format("{0} {1} {2}", _FieldLeft, ConvertSQLComparator(_Comparator), _FieldRigth);
      return String.Format("({0})", _Where.ToString());
    }
  }

  public class Between : IOperator
  {
    public Between(string pFieldLeft, string pParam1, string pParam2)
    {
      _FieldLeft = pFieldLeft;
      _Param1 = pParam1;
      _Param2 = pParam2;
    }

    private readonly string _FieldLeft;
    private readonly string _Param1;
    private readonly string _Param2;

    public override string ToString()
    {
      return String.Format("{0} BETWEEN {1} AND {2}", _FieldLeft, _Param1, _Param2);
    }
  }

  public class In : IOperator
  {
    public In(string pFieldLeft, params object[] pParams)
    {
      _FieldLeft = pFieldLeft;
      _Params = pParams.Select(Convert.ToString).ToArray();
    }

    public In(string pFieldLeft, string pRight)
    {
      _FieldLeft = pFieldLeft;
      _Right = pRight;
      _RightTo = true;
    }

    private readonly string _FieldLeft;
    private readonly object[] _Params;
    private readonly bool _RightTo;

    private string _Right
    {
      get;
      set;
    }

    protected virtual string Prefix()
    {
      return null;
    }

    public override string ToString()
    {
      if (_RightTo)
      {
        return String.Format("{0} {1} IN ({2})", _FieldLeft, Prefix(), _Right);
      }
      else
      {
        var prefix = Prefix();
        var parameters = String.Join(", ", _Params);
        if (prefix != null)
          return String.Format("{0} {1} IN ({2})", _FieldLeft, Prefix(), parameters);
        else
          return String.Format("{0} IN ({1})", _FieldLeft, parameters);
      }
    }
  }

  public class NotIn : In
  {
    public NotIn(string pFieldLeft, params object[] pParams)
      : base(pFieldLeft, pParams)
    {
    }

    public NotIn(string pFieldLeft, string pRight)
      : base(pFieldLeft, pRight)
    {
    }

    protected override string Prefix()
    {
      return "NOT";
    }
  }

  public class FormsOf
  {
    private readonly string _Phrase;

    public FormsOf(string pPhrase)
    {
      _Phrase = pPhrase;
    }

    public override string ToString()
    {
      return MakeCondition(_Phrase);
    }

    /// <summary>
    /// Quebrar a frase em palavras em espaços
    /// mantendo palavras entre " na mesma string
    /// e palavras com + entre elas na mesma string
    /// </summary>
    /// <param name="pPhrase"></param>
    /// <returns></returns>
    public static String[] BreakingHeadline(String pPhrase)
    {
      //remover espaços ao redor do + para evitar dividir palavras que possuem + entre elas
      while (pPhrase.IndexOf(" +", StringComparison.Ordinal) != -1)
        pPhrase = pPhrase.Replace(" +", "+");

      while (pPhrase.IndexOf("+ ", StringComparison.Ordinal) != -1)
        pPhrase = pPhrase.Replace("+ ", "+");

      //caso a quantidade de " presente na string seja impar, ignore a última
      if (System.Text.RegularExpressions.Regex.Matches(pPhrase, "\"").Count % 2 == 1)
      {
        int idx = pPhrase.LastIndexOf('"');

        pPhrase = pPhrase.Substring(0, idx) + pPhrase.Substring(idx + 1);
      }

      var partes = System.Text.RegularExpressions.Regex.Split(pPhrase, "\"([^\"]+)\"|\\s").Where(x => x.Length > 0).ToArray();
      var partesGrandes = partes.Where(x => x.Length > 2).ToArray();

      return partesGrandes.Length > 0 ? partesGrandes : partes;
    }

    public static String MakeCondition(String pPhrase)
    {
      var whereThesaurus = string.Empty;
      /*
       * Previne que strings que contenham apóstrofe inviabilizem a pesquinsa
       * do serviço por termos já inseridos em dúvidas posteriores
       *
       * obs: Correção do item [9.1.1] da [Lista de Correções 149 2016-06 Ronniery]
       */
      var wrappWord = new Func<string, string>(x =>
        x.Replace("'", "''")
         .Replace("*", @"\*\")
         .ToUpperInvariant()
      );
      var pharses = BreakingHeadline(pPhrase);
      foreach (var pharse in pharses)
      {
        if (String.IsNullOrEmpty(pharse))
          continue;
        //Se já foi adicionada alguma condição, devemos adicionar o OR antes de adicionar uma nova
        if (whereThesaurus.Length > 0)
          whereThesaurus += " OR ";

        if (pharse.Contains('+'))
        {
          // Se contiver + montar AND entre as palavras
          var sPartes = pharse.Split('+');
          var and = string.Empty;

          foreach (var word in sPartes.Where(word => !string.IsNullOrEmpty(word)))
          {
            if (and.Length > 0)
              and += " AND ";

            and += "FORMSOF(THESAURUS, \"" + wrappWord.Invoke(word) + "\" )";
          }

          whereThesaurus += and;
        }
        else
          whereThesaurus += "FORMSOF(THESAURUS, \"" + wrappWord.Invoke(pharse) + "\" )";
      }
      return whereThesaurus == String.Empty ? "formsof(THESAURUS, \"\")" : whereThesaurus;
    }
  }

  public class Contains : IOperator
  {
    public Contains(string pField, string pPharse, bool pUseFormsOf = true)
    {
      _Field = pField;
      _Pharse = pPharse;
      _UseFormsOf = pUseFormsOf;
    }

    private readonly string _Field;
    private readonly string _Pharse;
    private readonly bool _UseFormsOf;

    public FormsOf FormsOf
    {
      get
      {
        return new FormsOf(_Pharse);
      }
    }

    public override string ToString()
    {
      var pharse = _UseFormsOf ? "'" + FormsOf.ToString() + "'" : _Pharse;
      return string.Format("CONTAINS({0}, {1})", _Field, pharse);
    }

    public static implicit operator string(Contains v)
    {
      throw new NotImplementedException();
    }
  }

  public class Field
  {
    public Field()
    {
      Calculated = false;
    }

    public Field(string pName, string pNick = null)
      : this()
    {
      Name = pName;
      Nick = pNick;
    }

    public virtual String Name
    {
      get;
      private set;
    }

    public String Nick
    {
      get;
      private set;
    }

    public bool Calculated { get; set; }

  }

  public class Case<T> : IOperator
  {
    private List<IWhere> _wheres = new List<IWhere>();
    private List<T> _thens = new List<T>();
    private T _else;

    public Case<T> When(IWhere pWhere, T pThen)
    {
      _wheres.Add(pWhere);
      _thens.Add(pThen);
      return this;
    }

    public void Else(T pValue)
    {
      _else = pValue;
    }

    public Case()
    {
    }

    public override string ToString()
    {
      var sqlCase = "(CASE\r\n";
      for (int i = 0; i < _wheres.Count; i++)
        sqlCase += String.Format("\tWHEN {0} THEN {1}\r\n", _wheres[i].ToString(), _thens[i]);
      sqlCase += String.Format("\tELSE {0}\r\n", _else);
      sqlCase += "END)";
      return sqlCase;
    }

  }

  public class CaseField<T, S> : Field
  {
    private S _This;
    private Case<T> _Case;

    public CaseField(S pThis, string pNick)
      : base(string.Empty, pNick)
    {
      _Case = new Case<T>();
      _This = pThis;
      Calculated = true;
    }

    public CaseField(Case<T> pCase, string pNick)
      : base(string.Empty, pNick)
    {
      _Case = pCase;
      Calculated = true;
    }

    public CaseField<T, S> When(IWhere pWhere, T pThen)
    {
      _Case.When(pWhere, pThen);
      return this;
    }

    public S Else(T pValue)
    {
      _Case.Else(pValue);
      return _This;
    }

    public override string ToString()
    {
      return String.Format("{0} [{1}]", _Case.ToString(), this.Nick);
    }

  }

  public class Sum : Field
  {
    public Sum(string pName, string pNick = null)
      : base(pName, pNick)
    {
    }

    public override string Name
    {
      get
      {
        return String.Format("COALESCE(SUM({0}), 0)", base.Name);
      }
    }
  }

  public class Count : Field
  {
    public Count(string pName, string pNick = null)
      : base(pName, pNick)
    {
    }

    public override string Name
    {
      get
      {
        return String.Format("COALESCE(COUNT({0}), 0)", base.Name);
      }
    }
  }

  public class Max : Field
  {
    public Max(string pName, string pNick = null)
      : base(pName, pNick)
    {
    }

    public override string Name
    {
      get
      {
        return String.Format("MAX({0})", base.Name);
      }
    }
  }

  public class Min : Field
  {
    public Min(string pName, string pNick = null)
      : base(pName, pNick)
    {
    }

    public override string Name
    {
      get
      {
        return String.Format("MIN({0})", base.Name);
      }
    }
  }

  public class Avg : Field
  {
    public Avg(string pName, string pNick = null)
      : base(pName, pNick)
    {
    }

    public override string Name
    {
      get
      {
        return String.Format("AVG({0})", base.Name);
      }
    }
  }

  public class Join
  {
    public Join(string pEntity)
    {
      _Entity = pEntity;
    }

    public Join(string pEntity, string pNick)
    {
      _Entity = pEntity;
      _Nick = pNick;
    }

    public Join(string pEntity, IOperator pOperator)
      : this(pEntity)
    {
      _Operator = pOperator;
    }

    public Join(string pEntity, string pNick, IOperator pOperator)
      : this(pEntity, pOperator)
    {
      _Nick = pNick;
    }

    private readonly string _Entity;
    private readonly IOperator _Operator;
    private readonly string _Nick;

    protected virtual string TypeJoin
    {
      get
      {
        return "INNER";
      }
    }

    public override string ToString()
    {
      if (_Nick != null && _Operator != null)
        return String.Format("{0} JOIN {1} [{2}] ON {3}", TypeJoin, _Entity, _Nick, _Operator.ToString());
      if (_Nick == null && _Operator != null)
        return String.Format("{0} JOIN {1} ON {2}", TypeJoin, _Entity, _Operator.ToString());
      if (_Nick != null && _Operator == null)
        return String.Format("{0} JOIN {1} [{2}]", TypeJoin, _Entity, _Nick);
      return String.Format("{0} JOIN {1}", TypeJoin, _Entity);
    }
  }

  public class LeftJoin : Join
  {
    public LeftJoin(string pEntity, IOperator pOperator)
      : base(pEntity, pOperator)
    {
    }

    public LeftJoin(string pEntity, string pNick, IOperator pOperator)
      : base(pEntity, pNick, pOperator)
    {
    }

    protected override string TypeJoin
    {
      get
      {
        return "LEFT";
      }
    }
  }

  public class RightJoin : Join
  {
    public RightJoin(string pEntity, IOperator pOperator)
      : base(pEntity, pOperator)
    {
    }

    public RightJoin(string pEntity, string pNick, IOperator pOperator)
      : base(pEntity, pNick, pOperator)
    {
    }

    protected override string TypeJoin
    {
      get
      {
        return "RIGHT";
      }
    }

  }

  public class CrossJoin : Join
  {
    public CrossJoin(string pEntity)
      : base(pEntity)
    {
    }

    public CrossJoin(string pEntity, string pNick)
      : base(pEntity, pNick)
    {
    }

    protected override string TypeJoin
    {
      get
      {
        return "CROSS";
      }
    }

  }

  public interface IWhere : IOperator
  {
    IWhere Add(IOperator pConditional);

    IWhere Between(string pField, string pBeginParam, string pEndParam);

    IWhere In(string pField, params object[] pParams);

    IWhere NotIn(string pField, params object[] pParams);

    IWhere And(IOperator pCondicional = null);

    IWhere Not(IOperator pCondicional = null);

    IWhere Or(IOperator pCondicional = null);

    IWhere IsNull(string pField);

    IWhere Like(string pField, string pParameter);

    IWhere Contains(string pField, string pParameter);

    IWhere EqualsTo(string pField, string pParameter);

    IWhere NotEqualsTo(string pField, string pParameter);

    IWhere IsNotNull(string pField);

    IWhere GreaterThan(string pField, string pParameter);

    IWhere LessThan(string pField, string pParameter);

    IWhere GreaterThanOrEqual(string pField, string pParameter);

    IWhere LessThanOrEqual(string pField, string pParameter);

    IEnumerable<T> OfType<T>() where T : IOperator;

    int Count
    {
      get;
    }
  }

  public class Where : IWhere
  {
    private readonly List<IOperator> _Where = new List<IOperator>();

    public IWhere Add(IOperator pConditional)
    {
      _Where.Add(pConditional);
      return this;
    }

    public IWhere Between(string pField, string pBeginParam, string pEndParam)
    {
      var between = new Between(pField, pBeginParam, pEndParam);
      _Where.Add(between);
      return this;
    }

    public IWhere In(string pField, params object[] pParams)
    {
      var IN = new In(pField, pParams);
      _Where.Add(IN);
      return this;
    }

    public IWhere In(string pField, string pRight)
    {
      var IN = new In(pField, pRight);
      _Where.Add(IN);
      return this;
    }

    public IWhere NotIn(string pField, params object[] pParams)
    {
      var notIn = new NotIn(pField, pParams);
      _Where.Add(notIn);
      return this;
    }

    public IWhere NotIn(string pField, string pRight)
    {
      var notIn = new NotIn(pField, pRight);
      _Where.Add(notIn);
      return this;
    }

    public IWhere And(IOperator pCondicional = null)
    {
      if (_Where.Any())
        _Where.Add(new And());
      if (pCondicional != null)
        Add(pCondicional);
      return this;
    }

    public IWhere Not(IOperator pCondicional = null)
    {
      _Where.Add(new Not());
      if (pCondicional != null)
        Add(pCondicional);
      return this;
    }

    public IWhere Or(IOperator pCondicional = null)
    {
      if (_Where.Any())
        _Where.Add(new Or());
      if (pCondicional != null)
        Add(pCondicional);
      return this;
    }

    public IWhere IsNull(string pField)
    {
      Add(new Null(pField));
      return this;
    }

    public IWhere Like(string pField, string pParameter)
    {
      Add(new Comparator(pField, SQLComparator.Like, pParameter));
      return this;
    }

    public IWhere Contains(string pField, string pParameter)
    {
      Add(new Contains(pField, pParameter, false));
      return this;
    }

    public IWhere EqualsTo(string pField, string pParameter)
    {
      Add(new Comparator(pField, SQLComparator.Equal, pParameter));
      return this;
    }

    public IWhere IsNotNull(string pField)
    {
      Add(new NotNull(pField));
      return this;
    }

    public IWhere GreaterThan(string pField, string pParameter)
    {
      Add(new Comparator(pField, SQLComparator.GreaterThan, pParameter));
      return this;
    }

    public IWhere LessThan(string pField, string pParameter)
    {
      Add(new Comparator(pField, SQLComparator.LessThan, pParameter));
      return this;
    }

    public IWhere GreaterThanOrEqual(string pField, string pParameter)
    {
      Add(new Comparator(pField, SQLComparator.GreaterThanOrEqual, pParameter));
      return this;
    }

    public IWhere LessThanOrEqual(string pField, string pParameter)
    {
      Add(new Comparator(pField, SQLComparator.LessThanOrEqual, pParameter));
      return this;
    }

    public IWhere NotEqualsTo(string pField, string pParameter)
    {
      Add(new Comparator(pField, SQLComparator.NotEqual, pParameter));
      return this;
    }

    public IEnumerable<T> OfType<T>() where T : IOperator
    {
      return _Where.OfType<T>();
    }

    public int Count
    {
      get
      {
        return _Where.Count;
      }
    }

    public override string ToString()
    {
      return String.Format("({0})", String.Join(" ", _Where.Select(conditional => conditional.ToString()).ToArray())).Trim();
    }
  }

  public enum OrderType
  {
    None,
    Asc,
    Desc
  }

  public class OrderField
  {
    public OrderField(string pField, OrderType pOrderType = OrderType.None)
    {
      _Field = pField;
      _OrderType = pOrderType;
    }

    private readonly OrderType _OrderType;
    private readonly string _Field;

    public override string ToString()
    {
      return String.Format("{0} {1}", _Field, GetOrderType()).Trim();
    }

    private string GetOrderType()
    {
      switch (_OrderType)
      {
        case OrderType.None:
          return String.Empty;

        case OrderType.Asc:
          return "ASC";

        case OrderType.Desc:
          return "DESC";

        default:
          return String.Empty;
      }
    }

  }

  public class With : ISQLStatement
  {
    //Utilizados caso tenha apenas um AS
    private string _Name;

    private ISQLStatement _Statement;

    //Utilizado para mais de um AS
    private readonly List<Tuple<string, ISQLStatement>> _AsNamesSelects = new List<Tuple<string, ISQLStatement>>();

    private ISQLStatement _Select;

    public With As(string pName)
    {
      _Name = pName;
      if (_Statement != null)
        _AsNamesSelects.Add(new Tuple<string, ISQLStatement>(_Name, _Statement));
      return this;
    }

    public With Internal(ISQLStatement pStatement)
    {
      _Statement = pStatement;
      if (!string.IsNullOrEmpty(_Name))
        _AsNamesSelects.Add(new Tuple<string, ISQLStatement>(_Name, _Statement));
      return this;
    }

    public With As(string pName, ISQLStatement pStatement)
    {
      _AsNamesSelects.Add(new Tuple<string, ISQLStatement>(pName, pStatement));
      return this;
    }

    public With Select(ISQLStatement pSelect)
    {
      _Select = pSelect;
      return this;
    }

    public override string ToString()
    {
      var asWiths = String.Join("\r\n,", _AsNamesSelects.Select(CreateAsSQL).ToArray());
      return string.Format("WITH {0} {1}", asWiths, _Select.ToString()).Trim();
    }

    private static String CreateAsSQL(Tuple<string, ISQLStatement> pNameSelect)
    {
      return string.Format(" {0} AS ({1}) ", pNameSelect.Item1, pNameSelect.Item2.ToString());
    }
  }

  public class Select : ISQLStatement
  {

    private Tuple<String, String> _From;
    private readonly Dictionary<String, Field> _Fields = new Dictionary<String, Field>();
    private String _Distinct = String.Empty;
    private readonly List<Join> _Joins = new List<Join>();
    private IWhere _Where;
    private IWhere _Havind;
    private readonly List<string> _GroupBy = new List<string>();
    private readonly List<OrderField> _OrderBy = new List<OrderField>();
    private readonly List<KeyValuePair<String, Select>> _Union = new List<KeyValuePair<String, Select>>();
    private string _TopValue;
    private string _OffSet;

    public Select ClearAllFields()
    {
      _Fields.Clear();
      _GroupBy.Clear();
      _OrderBy.Clear();
      return this;
    }

    public Select Clone()
    {
      var select = new Select();
      select.From(_From.Item1, _From.Item2);

      foreach (var field in _Fields)
        select.Field(field.Value.Name, field.Value.Nick);

      select._Distinct = _Distinct;

      foreach (var join in _Joins)
        select._Joins.Add(join);

      select.Where(_Where);
      select.Having(_Havind);

      foreach (var groupBy in _GroupBy)
        select.GroupBy(groupBy);

      foreach (var orderBy in _OrderBy)
        select._OrderBy.Add(orderBy);

      foreach (var union in _Union)
        select._Union.Add(union);

      select._TopValue = _TopValue;
      select._OffSet = _OffSet;

      return select;
    }

    public Select AsEmpty(string pNick)
    {
      _Fields[pNick] = new Field("CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER)", pNick);
      return this;
    }

    public Select AsVarchar(string pName, string pNick)
    {
      _Fields[pNick] = new Field(String.Format("CAST({0} AS VARCHAR(MAX))", pName).Trim(), pNick);
      return this;
    }

    public Select Field(string pName, string pNick = null)
    {
      _Fields[pNick ?? pName] = new Field(pName, pNick);
      return this;
    }

    public Select Const(string pName, string pNick = null)
    {
      if (pNick != null)
      {
        _Fields[pNick] = new Field(String.Format("'{0}'", pName), pNick);
      }

      return this;
    }

    public Select Sum(string pName, string pNick = null)
    {
      _Fields[pNick ?? pName] = new Sum(pName, pNick);
      return this;
    }

    public Select Count(string pName, string pNick = null)
    {
      _Fields[pNick ?? pName] = new Count(pName, pNick);
      return this;
    }

    public Select Max(string pName, string pNick = null)
    {
      _Fields[pNick ?? pName] = new Max(pName, pNick);
      return this;
    }

    public Select Min(string pName, string pNick = null)
    {
      _Fields[pNick ?? pName] = new Min(pName, pNick);
      return this;
    }

    public Select Avg(string pName, string pNick = null)
    {
      _Fields[pNick ?? pName] = new Avg(pName, pNick);
      return this;
    }

    public Select Join(string pEntity, IOperator pConditional)
    {
      _Joins.Add(new Join(pEntity, pConditional));
      return this;
    }

    public Select Join(string pEntity, string pNick, IOperator pConditional)
    {
      _Joins.Add(new Join(pEntity, pNick, pConditional));
      return this;
    }

    public Select LeftJoin(string pEntity, IOperator pConditional)
    {
      _Joins.Add(new LeftJoin(pEntity, pConditional));
      return this;
    }

    public Select LeftJoin(string pEntity, string pNick, IOperator pConditional)
    {
      _Joins.Add(new LeftJoin(pEntity, pNick, pConditional));
      return this;
    }

    public Select RightJoin(string pEntity, IOperator pConditional)
    {
      _Joins.Add(new RightJoin(pEntity, pConditional));
      return this;
    }

    public Select RightJoin(string pEntity, string pNick, IOperator pConditional)
    {
      _Joins.Add(new RightJoin(pEntity, pNick, pConditional));
      return this;
    }

    public Select CrossJoin(string pEntity)
    {
      CrossJoin(pEntity, null);
      return this;
    }

    public Select CrossJoin(string pEntity, string pNick)
    {
      _Joins.Add(new CrossJoin(pEntity, pNick));
      return this;
    }

    public Select From(string pEntity, string pEntityNick = null)
    {
      _From = new Tuple<string, string>(pEntity, pEntityNick);
      return this;
    }

    public CaseField<T, Select> Case<T>(string pNick)
    {
      var fieldCase = new CaseField<T, Select>(this, pNick);
      _Fields[pNick] = fieldCase;
      return fieldCase;
    }

    public Select Case<T>(Case<T> pCase, string pNick)
    {
      var fieldCase = new CaseField<T, Select>(pCase, pNick);
      _Fields[pNick] = fieldCase;
      return this;
    }

    public Select Distinct()
    {
      _Distinct = "\n\tDISTINCT\n";
      return this;
    }

    public Select ClearWhere()
    {
      _Where = null;
      return this;
    }

    public Select Where(IWhere pWhere)
    {
      _Where = pWhere;
      return this;
    }

    public Select GroupBy(params string[] pField)
    {
      foreach (var field in pField)
        _GroupBy.Add(field);
      return this;
    }

    public Select Having(IWhere pWHere)
    {
      _Havind = pWHere;
      return this;
    }

    public Select OrderBy(string pField, OrderType pOrderType = OrderType.None)
    {
      _OrderBy.Add(new OrderField(pField, pOrderType));
      return this;
    }

    public Select SetOffSet(int pOffSet, int pRowCount)
    {
      _OffSet = String.Format("OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY", pOffSet, pRowCount).Trim();
      return this;
    }

    public Select UnionAll(Select pSelect)
    {
      _Union.Add(new KeyValuePair<String, Select>("UNION ALL", pSelect));
      return this;
    }

    public Select Union(Select pSelect)
    {
      _Union.Add(new KeyValuePair<String, Select>("UNION", pSelect));
      return this;
    }

    private static String CreateFieldSQL(Field pField)
    {
      if (!pField.Calculated)
      {
        if (pField.Nick != null)
          return String.Format("{0} [{1}]", pField.Name, pField.Nick).Trim();
        else
          return String.Format("{0}", pField.Name).Trim();
      }
      else
        return String.Format("{0}", pField.ToString()).Trim();
    }

    private string CreateFromSQL()
    {
      if (_From == null)
        return String.Empty;
      else if (_From.Item2 != null)
        return String.Format("FROM {0} [{1}]", _From.Item1, _From.Item2).Trim();
      else
        return String.Format("FROM {0}", _From.Item1).Trim();
    }

    public override string ToString()
    {
      var fields = String.Join("\r\n,", _Fields.Select(field => CreateFieldSQL(field.Value)).ToArray());
      var from = CreateFromSQL();

      var top = string.Empty;
      if (_TopValue != null)
        top = String.Format("\nTOP({0})\r\n", _TopValue);

      var joins = String.Empty;
      if (_Joins.Count > 0)
        joins = String.Join("\r\n", _Joins.Select(join => join.ToString()).ToArray());

      var where = String.Empty;
      if (_Where != null && _Where.Count > 0)
        where = String.Format("WHERE \r\n{0}", _Where.ToString());

      var groupBy = String.Empty;
      if (_GroupBy.Count > 0)
        groupBy = String.Format("GROUP BY \r\n{0}", String.Join("\r\n,", _GroupBy.Select(field => field).ToArray()));

      var orderBy = String.Empty;
      var offSet = String.Empty;
      if (_OrderBy.Count > 0)
      {
        orderBy = String.Format("ORDER BY \r\n{0}", String.Join("\r\n,", _OrderBy.Select(field => field.ToString()).ToArray()));
        if (_OffSet != null)
          offSet = _OffSet;
      }

      var having = String.Empty;
      if (_Havind != null)
        having = String.Format("HAVING \r\n{0}", _Havind.ToString());

      var union = String.Empty;
      if (_Union.Count > 0)
      {
        foreach (var u in _Union)
        {
          union += u.Key + "\r\n";
          union += u.Value.ToString() + "\r\n";
        }
      }

      return String.Format("SELECT{8}{9}\r\n{0}\r\n{1}\r\n{2} \r\n{3} \r\n{4} \r\n{5} \r\n{6} \r\n{7} \r\n{10}", fields, from, joins, where, groupBy, having, orderBy, offSet, _Distinct, top, union).Trim();
    }

    public Select Top(int topValue)
    {
      _TopValue = Convert.ToString(topValue);
      return this;
    }

    public Select Top(string topValue)
    {
      _TopValue = topValue;
      return this;
    }
  }
}