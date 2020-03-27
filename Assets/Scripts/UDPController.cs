//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using System.Net;
//using System.Net.Sockets;
//using System.Threading;
//
//public class UDPController
//{
//	//
//	// 소켓 접속 관련.
//	//
//
//	// 클라이언트와의 접속용 소켓.
//	private Socket			_socket = null;
//
//	// 송신 버퍼.
//	private PacketQueue		_sendQueue;
//	
//	// 수신 버퍼.
//	private PacketQueue		_recvQueue;
//	
//	// 서버 플래그.	
//	private bool	 		_isServer = false;
//
//	// 접속 플래그.
//	private	bool			_isConnected = false;
//
//	//
//	// 이벤트 관련 멤버 변수.
//	//
//
//	// 이벤트 통지 델리게이트.
//	public delegate void 	EventHandler(NetEventState state);
//
//	private EventHandler	_handler;
//
//	//
//	// 스레드 관련 멤버 변수.
//	//
//
//	// 스레드 실행 플래그.
//	protected bool			_threadLoop = false;
//	
//	protected Thread		_thread = null;
//
//	private static int 		_mtu = 1400;
//
//
//	// Use this for initialization
//	public UDPController ()
//    {
//        // 송수신 버퍼를 만듭니다.
//        _sendQueue = new PacketQueue();
//        _recvQueue = new PacketQueue();	
//	}
//	
//
//	// 대기시작.
//	public bool StartServer(int port, int connectionNum)
//	{
//        Debug.Log("StartServer called.!");
//
//        // 리스닝 소켓을 생성합니다. 
//        try 
//        {
//			// 소켓을 생성합니다.
//			_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//			// 사용할 포트 번호를 할당합니다.
//			_socket.Bind(new IPEndPoint(IPAddress.Any, port));
//        }
//        catch {
//			Debug.Log("StartServer fail");
//            return false;
//        }
//
//        _isServer = true;
//
//        return LaunchThread();
//    }
//
//	// 대기 종료 .
//    public void StopServer()
//    {
//		_threadLoop = false;
//        if (_thread != null) 
//        {
//            _thread.Join();
//            _thread = null;
//        }
//
//        Disconnect();
//
//		if (_socket != null) 
//		{
//			_socket.Close();
//			_socket = null;
//        }
//
//        _isServer = false;
//
//        Debug.Log("Server stopped.");
//    }
//
//
//    // 접속.
//    public bool Connect(string address, int port)
//    {
//        Debug.Log("TransportTCP connect called.");
//
//		if (_socket != null) 
//		{
//            return false;
//        }
//
//		bool ret = false;
//        try 
//        {
//			_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//
//            _socket.NoDelay = true;
//            _socket.Connect(address, port);
//			ret = LaunchThread();
//		}
//        catch 
//        {
//            _socket = null;
//        }
//
//		if (ret == true) 
//		{
//			_isConnected = true;
//			Debug.Log("Connection success.");
//		}
//		else 
//		{
//			_isConnected = false;
//			Debug.Log("Connect fail");
//		}
//
//        if (_handler != null) 
//        {
//            // 접속 결과를 통지합니다. 
//			NetEventState state = new NetEventState();
//			state.type = NetEventType.Connect;
//			state.result = (_isConnected == true) ? NetEventResult.Success : NetEventResult.Failure;
//            _handler(state);
//			Debug.Log("event handler called");
//        }
//
//        return _isConnected;
//    }
//
//	// 끊기.
//    public void Disconnect() 
//    {
//        _isConnected = false;
//
//        if (_socket != null) 
//        {
//            // 소켓 클로즈.
//            _socket.Shutdown(SocketShutdown.Both);
//            _socket.Close();
//            _socket = null;
//        }
//
//        // 끊기를 통지합니다.
//        if (_handler != null) 
//        {
//			NetEventState state = new NetEventState();
//			state.type = NetEventType.Disconnect;
//			state.result = NetEventResult.Success;
//			_handler(state);
//        }
//    }
//
//    // 송신처리.
//    public int Send(byte[] data, int size)
//	{
//		if (_sendQueue == null) 
//		{
//			return 0;
//		}
//
//        return _sendQueue.Enqueue(data, size);
//    }
//
//    // 수신처리.
//    public int Receive(ref byte[] buffer, int size)
//	{
//		if (_recvQueue == null) 
//		{
//			return 0;
//		}
//
//        return _recvQueue.Dequeue(ref buffer, size);
//    }
//
//	// 이벤트 통지 함수 등록.
//    public void RegisterEventHandler(EventHandler handler)
//    {
//        _handler += handler;
//    }
//
//	// 이벤트 통지 함수 삭제.
//    public void UnregisterEventHandler(EventHandler handler)
//    {
//        _handler -= handler;
//    }
//
//	// 스레드 실행 함수.
//	bool LaunchThread()
//	{
//		try 
//		{
//			// Dispatch용 스레드 시작.
//			_threadLoop = true;
//			_thread = new Thread(new ThreadStart(Dispatch));
//			_thread.Start();
//		}
//		catch 
//		{
//			Debug.Log("Cannot launch thread.");
//			return false;
//		}
//		
//		return true;
//	}
//
//	// 스레드의 송수신 처리.
//    public void Dispatch()
//	{
//		Debug.Log("Dispatch thread started.");
//
//		while (_threadLoop) 
//		{
//
//			// 클라이언트와의 송수신을 처리합니다.
//			if (_socket != null && _isConnected == true) 
//			{
//				// 송신처리.
//	            DispatchSend();
//
//	            // 수신처리.
//	            DispatchReceive();
//	        }
//
//			Thread.Sleep(5);
//		}
//
//		Debug.Log("Dispatch thread ended.");
//    }
//
//	// 스레드의 송수신처리.
//    void DispatchSend()
//	{
//        try 
//        {
//            // 송신처리.
//            if (_socket.Poll(0, SelectMode.SelectWrite)) 
//            {
//				byte[] buffer = new byte[_mtu];
//
//                int sendSize = _sendQueue.Dequeue(ref buffer, buffer.Length);
//                while (sendSize > 0) 
//                {
//                    _socket.Send(buffer, sendSize, SocketFlags.None);
//                    sendSize = _sendQueue.Dequeue(ref buffer, buffer.Length);
//                }
//            }
//        }
//        catch 
//        {
//            return;
//        }
//    }
//
//	// 스레드의 수신처리.
//    void DispatchReceive()
//	{
//        // 수신처리.
//        try {
//            while (_socket.Poll(0, SelectMode.SelectRead)) 
//            {
//				byte[] buffer = new byte[_mtu];
//
//                int recvSize = _socket.Receive(buffer, buffer.Length, SocketFlags.None);
//                if (recvSize == 0) 
//                {
//                    // 끊기.
//                    Debug.Log("Disconnect recv from client.");
//                    Disconnect();
//                }
//                else if (recvSize > 0) 
//                {
//                    _recvQueue.Enqueue(buffer, recvSize);
//                }
//            }
//        }
//        catch 
//        {
//            return;
//        }
//    }
//
//	// 서버인지 확인.
//	public bool IsServer() 
//	{
//		return _isServer;
//	}
//	
//    // 접속 확인.
//    public bool IsConnected() 
//    {
//        return _isConnected;
//    }
//}
