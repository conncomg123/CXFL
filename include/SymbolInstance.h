#ifndef SYMBOLINSTANCE_H
#define SYMBOLINSTANCE_H

#include "Element.h"
#include "Matrix.h"
#include "Point.h"
#include <optional>

class SymbolInstance : public Element, private Matrix, private Point {
private:
	pugi::xml_node root;
	std::string libraryItemName;
	bool selected;
	std::string symbolType;
	unsigned int firstFrame;
	std::optional<unsigned int> lastFrame;
	std::string loop;
	double getWidthRecur() const;
	double getHeightRecur() const;
public:
	SymbolInstance(pugi::xml_node& elementNode);
	~SymbolInstance();
	SymbolInstance(SymbolInstance& symbolInstance);
	std::string getLibraryItemName();
	void setLibraryItemName(const std::string& libraryItemName);
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
	Matrix* getMatrix();
	Point* getPoint();
};

#endif // SYMBOLINSTANCE_H