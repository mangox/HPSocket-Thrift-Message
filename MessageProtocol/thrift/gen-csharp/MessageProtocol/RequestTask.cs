/**
 * Autogenerated by Thrift Compiler (0.10.0)
 *
 * DO NOT EDIT UNLESS YOU ARE SURE THAT YOU KNOW WHAT YOU ARE DOING
 *  @generated
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Thrift;
using Thrift.Collections;
using System.Runtime.Serialization;
using Thrift.Protocol;
using Thrift.Transport;

namespace MessageProtocol
{

  #if !SILVERLIGHT
  [Serializable]
  #endif
  public partial class RequestTask : TBase
  {
    private string _clientId;
    private string _version;
    private string _message;

    public string ClientId
    {
      get
      {
        return _clientId;
      }
      set
      {
        __isset.clientId = true;
        this._clientId = value;
      }
    }

    public string Version
    {
      get
      {
        return _version;
      }
      set
      {
        __isset.version = true;
        this._version = value;
      }
    }

    public string Message
    {
      get
      {
        return _message;
      }
      set
      {
        __isset.message = true;
        this._message = value;
      }
    }


    public Isset __isset;
    #if !SILVERLIGHT
    [Serializable]
    #endif
    public struct Isset {
      public bool clientId;
      public bool version;
      public bool message;
    }

    public RequestTask() {
    }

    public void Read (TProtocol iprot)
    {
      iprot.IncrementRecursionDepth();
      try
      {
        TField field;
        iprot.ReadStructBegin();
        while (true)
        {
          field = iprot.ReadFieldBegin();
          if (field.Type == TType.Stop) { 
            break;
          }
          switch (field.ID)
          {
            case 1:
              if (field.Type == TType.String) {
                ClientId = iprot.ReadString();
              } else { 
                TProtocolUtil.Skip(iprot, field.Type);
              }
              break;
            case 2:
              if (field.Type == TType.String) {
                Version = iprot.ReadString();
              } else { 
                TProtocolUtil.Skip(iprot, field.Type);
              }
              break;
            case 3:
              if (field.Type == TType.String) {
                Message = iprot.ReadString();
              } else { 
                TProtocolUtil.Skip(iprot, field.Type);
              }
              break;
            default: 
              TProtocolUtil.Skip(iprot, field.Type);
              break;
          }
          iprot.ReadFieldEnd();
        }
        iprot.ReadStructEnd();
      }
      finally
      {
        iprot.DecrementRecursionDepth();
      }
    }

    public void Write(TProtocol oprot) {
      oprot.IncrementRecursionDepth();
      try
      {
        TStruct struc = new TStruct("RequestTask");
        oprot.WriteStructBegin(struc);
        TField field = new TField();
        if (ClientId != null && __isset.clientId) {
          field.Name = "clientId";
          field.Type = TType.String;
          field.ID = 1;
          oprot.WriteFieldBegin(field);
          oprot.WriteString(ClientId);
          oprot.WriteFieldEnd();
        }
        if (Version != null && __isset.version) {
          field.Name = "version";
          field.Type = TType.String;
          field.ID = 2;
          oprot.WriteFieldBegin(field);
          oprot.WriteString(Version);
          oprot.WriteFieldEnd();
        }
        if (Message != null && __isset.message) {
          field.Name = "message";
          field.Type = TType.String;
          field.ID = 3;
          oprot.WriteFieldBegin(field);
          oprot.WriteString(Message);
          oprot.WriteFieldEnd();
        }
        oprot.WriteFieldStop();
        oprot.WriteStructEnd();
      }
      finally
      {
        oprot.DecrementRecursionDepth();
      }
    }

    public override string ToString() {
      StringBuilder __sb = new StringBuilder("RequestTask(");
      bool __first = true;
      if (ClientId != null && __isset.clientId) {
        if(!__first) { __sb.Append(", "); }
        __first = false;
        __sb.Append("ClientId: ");
        __sb.Append(ClientId);
      }
      if (Version != null && __isset.version) {
        if(!__first) { __sb.Append(", "); }
        __first = false;
        __sb.Append("Version: ");
        __sb.Append(Version);
      }
      if (Message != null && __isset.message) {
        if(!__first) { __sb.Append(", "); }
        __first = false;
        __sb.Append("Message: ");
        __sb.Append(Message);
      }
      __sb.Append(")");
      return __sb.ToString();
    }

  }

}
