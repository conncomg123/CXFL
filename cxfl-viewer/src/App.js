import { Button } from 'antd';
import { PlusOutlined, FolderAddOutlined, DeleteOutlined, EyeInvisibleOutlined, LockOutlined } from '@ant-design/icons';
import SplitPane from 'react-split-pane';
import './App.css';

function App() {
  return (
    <html lang="en">
      <div>
        <div style={{ display: 'flex', justifyContent: 'space-between', padding: '20px' }}>
          <SplitPane split="vertical" minSize={250} defaultSize="33%">
            <div>
              <Button type="default" icon={<PlusOutlined />}></Button>
              <Button type="default" icon={<FolderAddOutlined />}></Button>
              <Button type="default" icon={<DeleteOutlined />}></Button>
            </div>
            <div>
              <Button type="default" icon={<EyeInvisibleOutlined />}></Button>
              <Button type="default" icon={<LockOutlined />}></Button>
            </div>
          </SplitPane>
        </div>
      </div>
    </html>
  );
}

export default App;
