#ifndef _GAMESEN_CIRCULAR_BUFFER_H_
#define _GAMESEN_CIRCULAR_BUFFER_H_
#include <string>
#include <array>

#define min(a,b) ((a)<(b)?(a):(b))

namespace gamesen {

template<typename T, typename IndexType = int>
class basic_circular_buffer
{
public:
	basic_circular_buffer()
		: _array(new T[_DefaultCapacity])
		, _arrayLength(_DefaultCapacity)
		, _head(0)
		, _tail(0)
		, _size(0)
		, _version(0)
	{
	}

	basic_circular_buffer(IndexType capacity)
		: _array(new T[capacity])
		, _arrayLength(capacity)
		, _head(0)
		, _tail(0)
		, _size(0)
		, _version(0)
	{
	}

	basic_circular_buffer(T* data, IndexType count)
		: _head(0)
		, _version(0)
		, _array(nullptr)
	{
		IndexType length(_DefaultCapacity);
		if (count > _DefaultCapacity)
		{
			length = count + _MinimumGrow;
		}
		allocateArray(length);
		memcpy(_array, data, sizeof(T) * count);

		_tail = count;
		_size = count;
	}

	basic_circular_buffer(const basic_circular_buffer<T>& other)
		: _head(other._head)
		, _tail(other._tail)
		, _size(other._size)
		, _version(0)
	{
		allocateArray(other._arrayLength);
		memcpy(_array, other._array, other._size);
	}

	basic_circular_buffer& operator=(const basic_circular_buffer<T>& other)
	{
		_head = other._head;
		_tail = other._tail;
		_size = other._size;
		_version = (0);
		allocateArray(other._arrayLength);
		memcpy(_array, other._array, other._size);
		return *this;
	}

	~basic_circular_buffer()
	{
		if (_array != nullptr)
		{
			delete[] _array;
			_array = nullptr;
		}
	}

	IndexType size()
	{
		return _size;
	}

	void clear()
	{
		_head = 0;
		_tail = 0;
		_size = 0;
		_version = _version + 1;
	}

	bool empty() { return _size == 0; }

	/*
	Get first item
	*/
	T& dequeue()
	{
		if (_size == 0)
		{
			throw std::exception("Empty queue");
		}
		T& t = _array[_head];

		_array[_head] = T();
		_head = (_head + 1) % _arrayLength;
		--_size;
		++_version;
		return t;
	}

	/*
	Add item to the end.
	*/
	void enqueue(const T& item)
	{
		if (_size == _arrayLength)
		{
			int length = (IndexType)((long)((long)_arrayLength) * (long)_GrowFactor / (long)100);
			if (length < _arrayLength + _MinimumGrow)
			{
				length = _arrayLength + _MinimumGrow;
			}
			setCapacity(length);
		}
		_array[_tail] = item;
		_tail = (_tail + 1) % (int)_arrayLength;
		++_size;
		++_version;
	}

	/*
	Add items to the end.
	*/
	void enqueue(const T* items, IndexType size)
	{
		if (size == 0)
		{
			return;
		}

		if (items == nullptr)
		{
			throw std::exception("Argument null: items");
		}

		for (IndexType i = 0; i < size; ++i)
		{
			enqueue(items[i]);
		}
	}

	/*
	Adds item to the head of buffer, preserve its order
	*/
	void pushHead(const T& item)
	{
		if (_size == (int)_arrayLength)
		{
			int length = (IndexType)((long)((long)_arrayLength) * (long)_GrowFactor / (long)100);
			if (length < _arrayLength + _MinimumGrow)
			{
				length = _arrayLength + _MinimumGrow;
			}
			setCapacity(length);
		}
		int length1 = _arrayLength;
		_head = (_head - 1 + length1) % length1;
		_array[_head] = item;
		++_size;
		++_version;
	}

	/*
	Adds number of items to the head of buffer, preserve its order
	*/
	void pushHead(const T* items, IndexType size)
	{
		if (size == 0)
		{
			return;
		}

		if (items == nullptr)
		{
			throw std::exception("Argument null: items");
		}

		for (IndexType i = size - 1; i >= 0; --i)
		{
			pushHead(items[i]);
		}
	}

	T& operator[](IndexType index)
	{
		if (index >= _size)
		{
			throw std::exception("Index out of range");
		}

		return _array[(_head + index) % _arrayLength];
	}

	/*
	Gets raw data that this buffer holds
	*/
	T* getData()
	{
		return getData(_size);
	}

	IndexType copyTo(T* dest, IndexType destLength, IndexType destIndex, IndexType offset, IndexType count)
	{
		if (dest == nullptr)
		{
			throw std::exception("dest null");
		}
		if (destIndex < 0 || destIndex > destLength)
		{
			throw std::exception("destIndex out of range");
		}
		int length = destLength;
		if (_size == 0)
		{
			return 0;
		}
		count = min(count, length - destIndex);
		count = min(count, _size - offset);

		int num = count;
		int num1 = ((int)_arrayLength - _head - offset < num ? (int)_arrayLength - _head - offset : num);
		memcpy(dest + destIndex, _array + _head + offset, num1 * sizeof(T));
		//Array.Copy(_array, _head + offset, dest, destIndex, num1);
		num = num - num1;
		if (num > 0)
		{
			memcpy(dest + destIndex + num1, _array, num * sizeof(T));
			//Array.Copy(_array, 0, dest, destIndex + num1, num);
		}

		return count;
	}

private:
	void allocateArray(IndexType capacity)
	{
		if (_array != nullptr)
		{
			delete[] _array;
		}
		_arrayLength = capacity;
		_array = new T[_arrayLength];
	}

	T* getData(IndexType capacity)
	{
		T* tArray = new T[capacity];
		if (_head >= _tail)
		{
			memcpy(tArray, _array + _head, sizeof(T) * (_arrayLength - _head));
			memcpy(tArray + (_arrayLength - _head), _array, sizeof(T) * _tail);
		}
		else
		{
			memcpy(tArray, _array + _head, sizeof(T) * _size);
		}

		return tArray;
	}

	void setCapacity(IndexType capacity)
	{
		T* tArray = getData(capacity);
		delete[] _array;
		_array = tArray;
		_arrayLength = capacity;
		_head = 0;
		_tail = (_size == capacity ? 0 : _size);
		_version = _version + 1;
	}

protected:
	T* _array;
	IndexType _arrayLength;
	IndexType _head;
	IndexType _tail;
	IndexType _size;
	unsigned int _version;

	static const IndexType _MinimumGrow = 4;
	static const IndexType _ShrinkThreshold = 128;
	static const IndexType _GrowFactor = 200;
	static const IndexType _DefaultCapacity = 512;
};

typedef basic_circular_buffer<unsigned char> CircularBuffer;

}
#endif