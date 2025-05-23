using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Operation {
    public static char GetOperatorTypeChar(OperatorType op) {
        switch(op) {
            case OperatorType.Add:
                return '+';
            case OperatorType.Subtract:
                return '-';
            case OperatorType.Multiply:
                return 'x';
            case OperatorType.Divide:
                return '÷';
            case OperatorType.Equal:
                return '=';
            default:
                return ' ';
        }
    }

    public static string GetOperatorTypeString(OperatorType op) {
        switch(op) {
            case OperatorType.Add:
                return "+";
            case OperatorType.Subtract:
                return "-";
            case OperatorType.Multiply:
                return "x";
            case OperatorType.Divide:
                return "÷";
            case OperatorType.Equal:
                return "=";
            default:
                return "";
        }
    }

    public int operand1;
    public OperatorType op;
    public int operand2;

    public int equal {
        get {
            switch(op) {
                case OperatorType.Add:
                    return operand1 + operand2;
                case OperatorType.Subtract:
                    return operand1 - operand2;
                case OperatorType.Multiply:
                    return operand1 * operand2;
                case OperatorType.Divide:
                    return operand2 != 0 ? operand1 / operand2 : 0;
                default:
                    return 0;
            }
        }
    }

    public string opText { get { return GetOperatorTypeString(op); } }

    /// <summary>
    /// Return operation as a string, e.g. "1 + 2 ="
    /// </summary>
    public string GetUnsolvedString() {
        var sb = new System.Text.StringBuilder();

        sb.Append(operand1);
        sb.Append(' ');
        sb.Append(GetOperatorTypeChar(op));
        sb.Append(' ');
		sb.Append(operand2);
		sb.Append(" =");

        return sb.ToString();
	}
}
