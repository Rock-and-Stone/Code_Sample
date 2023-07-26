using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Jobs;
using System.Diagnostics;

public class JobExample : MonoBehaviour
{
	[SerializeField]
	private GameObject _gab;

	public bool _isUsingJob;

	private GameObject[] prefabs = new GameObject[10000];

	private float _fSpeed = 0;

	private NativeArray<Vector3> _positions;

	private JobHandle _jobHandle;

	void Start()
	{
		_positions = new NativeArray<Vector3>(10000, Allocator.TempJob);
		for (int i = 0; i < 10000; i++)
		{
			prefabs[i] = Instantiate(_gab, this.transform);
		}
	}
	void Update()
	{
		if( _isUsingJob )
		{
			var updatePositionsJob = new UpdatePositionsJob()
			{
				positions = _positions,
				deltaTime = Time.deltaTime,
				speed = _fSpeed
			};


			// TransformAccessArray 만들고
			var transforms = new TransformAccessArray(10000);
			//var transforms = new TransformAccessArray(transform.childCount);
			var stopwatch = Stopwatch.StartNew();
			for (int i = 0; i < transform.childCount; i++)
			{
				transforms.Add(transform.GetChild(i));
			}
			// 잡 스케쥴 잡고
			_jobHandle = updatePositionsJob.Schedule(transforms);

			JobHandle.ScheduleBatchedJobs();
			// 잡 다 완성할때 까지 기다리고
			_jobHandle.Complete();

			// 포지션을 잡에서 갖고온 값으로 바꿔주고
			for (int i = 0; i < transform.childCount; i++)
			{
				Transform childTransform = transform.GetChild(i);
				childTransform.position = _positions[i];

			}

			stopwatch.Stop();
			UnityEngine.Debug.Log($"총 걸린 시간 :  {stopwatch.Elapsed.TotalMilliseconds} ms");
			_fSpeed += Time.deltaTime;

			// Array 해제해주고
			transforms.Dispose();
		}
		else
		{

			for (int i = 0; i < 10000; i++)
			{
				prefabs[i].transform.position += new Vector3(_fSpeed, 0f, 0f) * Time.deltaTime;
			}

			_fSpeed += Time.deltaTime;
		}
	}


	private void OnDestroy()
	{
		// NativeArray 메모리 해제
		if (_positions.IsCreated)
		{
			_positions.Dispose();
		}
	}

	// 오브젝트의 포지션을 업데이트하기 위한 잡
	struct UpdatePositionsJob : IJobParallelForTransform
	{
		public NativeArray<Vector3> positions;
		public float deltaTime;
		public float speed;

		public void Execute(int index, TransformAccess transform)
		{
			Vector3 position = transform.position;
			position += new Vector3(speed, 0f, 0f) * deltaTime;
			positions[index] = position;
		}
	}

}
