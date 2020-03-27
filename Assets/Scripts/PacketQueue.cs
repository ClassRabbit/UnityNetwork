using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

public class PacketQueue
{	
    // 패킷 저장 정보.
    struct PacketInfo
    {
        public int	offset;
        public int 	size;
    };
	
    //
    private MemoryStream 		_streamBuffer;
	
    private List<PacketInfo>	_offsetList;
	
    private int					_offset = 0;


    private Object lockObj = new Object();
	
    //  생성자(여기서 초기화합니다).
    public PacketQueue()
    {
        _streamBuffer = new MemoryStream();
        _offsetList = new List<PacketInfo>();
    }
	
    // 큐를 추가합니다.
    public int Enqueue(byte[] data, int size)
    {
        PacketInfo	info = new PacketInfo();
	
        info.offset = _offset;
        info.size = size;
			
        lock (lockObj) 
        {
            // 패킷 저장 정보를 보존.
            _offsetList.Add(info);
			
            // 패킷 데이터를 보존.
            _streamBuffer.Position = _offset;
            _streamBuffer.Write(data, 0, size);
            _streamBuffer.Flush();
            _offset += size;
        }
		
        return size;
    }
	
    // 큐를 꺼냅니다.
    public int Dequeue(ref byte[] buffer, int size) {

        if (_offsetList.Count <= 0) {
            return -1;
        }

        int recvSize = 0;
        lock (lockObj) 
        {	
            PacketInfo info = _offsetList[0];
		
            // 버퍼로부터 해당하는 패킷 데이터를 가져옵니다.
            int dataSize = Math.Min(size, info.size);
            _streamBuffer.Position = info.offset;
            recvSize = _streamBuffer.Read(buffer, 0, dataSize);
			
            // 큐 데이터를 꺼냈으므로 선두 요소 삭제.
            if (recvSize > 0) 
            {
                _offsetList.RemoveAt(0);
            }
			
            // 모든 큐 데이터를 꺼냈을 때는 스트림을 클리어해서 메모리를 절약합니다.
            if (_offsetList.Count == 0) 
            {
                Clear();
                _offset = 0;
            }
        }
		
        return recvSize;
    }

    // 큐를 클리어합니다.	
    public void Clear()
    {
        byte[] buffer = _streamBuffer.GetBuffer();
        Array.Clear(buffer, 0, buffer.Length);
		
        _streamBuffer.Position = 0;
        _streamBuffer.SetLength(0);
    }
}