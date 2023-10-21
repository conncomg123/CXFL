#ifndef SYMBOLINSTANCE_H
#define SYMBOLINSTANCE_H

#include "Instance.h"
#include "Matrix.h"
#include "Point.h"
#include <optional>
#include <memory>

class SymbolInstance : public Instance {
private:
	Matrix matrix;
	Point point;
	bool selected;
	unsigned int firstFrame;
	std::optional<unsigned int> lastFrame;
	std::string symbolType;
	std::string loop;
	double getWidthRecur() const;
	double getHeightRecur() const;
public:
	SymbolInstance(pugi::xml_node& elementNode);
	~SymbolInstance() override;
	SymbolInstance(SymbolInstance& symbolInstance);
	bool isSelected();
	void setSelected(bool selected);
	std::string getSymbolType();
	void setSymbolType(const std::string& symbolType);
	unsigned int getFirstFrame();
	void setFirstFrame(unsigned int firstFrame);
	std::optional<unsigned int> getLastFrame();
	void setLastFrame(unsigned int lastFrame);
	void setLastFrame(std::optional<unsigned int> lastFrame);
	std::string getLoop();
	void setLoop(const std::string& loop);
	double getWidth() const override;
	double getHeight() const override;
	Matrix& getMatrix();
	Point& getPoint();
};

#endif // SYMBOLINSTANCE_H