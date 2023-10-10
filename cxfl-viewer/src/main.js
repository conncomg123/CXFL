import { Button, Divider, Table } from 'antd';
import { PlusOutlined, FolderAddOutlined, DeleteOutlined, EyeInvisibleOutlined, LockOutlined } from '@ant-design/icons';
import SplitPane from 'react-split-pane';
import Timeline from './jsx_components/Timeline.jsx';
import './globalStyles.css';

const columns = [
  {
    title: 'Column 1',
    dataIndex: 'column1',
    key: 'column1',
  },
  {
    title: 'Column 2',
    dataIndex: 'column2',
    key: 'column2',
    width: 15, // Set a fixed width of 50px for Column 2
  },
  {
    title: 'Column 3',
    dataIndex: 'column3',
    key: 'column3',
    width: 15, // Set a fixed width of 50px for Column 3
  },
];

const data = [
  {
    key: '1',
    column1: 'Layer 1',
    column2: <Button type="text" icon={<EyeInvisibleOutlined />} />,
    column3: <Button type="text" icon={<LockOutlined />} />,
  },
  {
    key: '2',
    column1: 'Layer 2',
    column2: <Button type="text" icon={<EyeInvisibleOutlined />} />,
    column3: <Button type="text" icon={<LockOutlined />} />,
  },
  {
    key: '3',
    column1: 'Layer 3',
    column2: <Button type="text" icon={<EyeInvisibleOutlined />} />,
    column3: <Button type="text" icon={<LockOutlined />} />,
  },
];

const TLDisplayData = [
  {
    key: '1',
    column1: <Timeline data="BEEBEEKFFKFF" />,
  },
  {
    key: '2',
    column1: <Timeline data="KFFFFFFFFFFKFFFKFKFFKFKKKFFKFFFBEEEEEEEEEEEEEEEEEKFFFFFFFFKFFFFFKFKFKFBEEEEEEEEEEE" />,
  },
  {
    key: '3',
    column1: <Timeline data="BEEBEEKFFKFF" />,
  },
];

const tablePagination = {
  hideOnSinglePage: true,
  disabled: true
};

const CustomTable = ({ columns, dataSource, pagination }) => {
  return (
    <Table
      columns={columns}
      dataSource={dataSource}
      pagination={pagination}
    />
  );
};

function App() {
  return (
    <html lang="en">
      <div className="container">
        <SplitPane split="vertical" minSize={300} defaultSize="33%">
          <div className="left-pane">
            <div className="quadrant quadrant1">Quadrant 1</div>
            <div className="quadrant quadrant3">
              <Table columns={columns} dataSource={data} pagination={tablePagination} showHeader={false} />
            </div>
          </div>
          <div className="right-pane">
            <div className="quadrant quadrant2">Quadrant 2</div>
            <div className="quadrant quadrant4">
                <Table className="timelineDisplay" columns={columns} dataSource={TLDisplayData} pagination={tablePagination} showHeader={false} />
              </div>
          </div>
        </SplitPane>
      </div>
    </html>
  );
}

export default App;